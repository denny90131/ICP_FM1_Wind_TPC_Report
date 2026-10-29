using System.DirectoryServices.AccountManagement; // 捕捉 LDAP/AD 專用例外
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("Auth")]
public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private const string DefaultRedirectUrl = "/api/WindFarm/ForecastDataSummary";

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    // 轉跳登入頁面
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        //如果使用者有cookie驗證，則直接轉跳目標頁面
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginRequest { returnUrl = returnUrl });
    }

    // AD 網域登入
    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest loginRequest)
    {
        ViewData["ReturnUrl"] = loginRequest.returnUrl;

        if (string.IsNullOrWhiteSpace(loginRequest.Username) || string.IsNullOrWhiteSpace(loginRequest.Password))
        {
            ViewBag.ErrorMessage = "Please enter both username and password.";
            ViewBag.Username = loginRequest.Username;
            return View(loginRequest);
        }

        try
        {
            // 如果有打詳細\\，則自動省略前面網域開頭
            string cleanUsername = loginRequest.Username.Contains('\\') ? loginRequest.Username.Split('\\')[1] : loginRequest.Username;
            // 依照使用者名稱及密碼進行 AD登入
            LoginResponse Login_response = await _authService.LoginWithAdAsync(cleanUsername, loginRequest.Password);

            //登入失敗
            if (!Login_response.LoginStatus)
            {
                //返還錯誤信息及使用者帳號
                ViewBag.ErrorMessage = string.IsNullOrEmpty(Login_response.ErrorMessage) ? "Invalid username or password. Please try again." : Login_response.ErrorMessage;
                ViewBag.Username = loginRequest.Username;
                return View(loginRequest);
            }

            // 並建立cookie驗證(附帶權限管控)
            await SignInUserAsync(cleanUsername, Login_response.Permission);
            return RedirectToLocal(loginRequest.returnUrl);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "LDAP/AD server is unavailable (Login)");
            ViewBag.ErrorMessage = "Unable to connect to LDAP/AD domain authentication server. Please contact your system administrator.";
            ViewBag.Username = loginRequest.Username;
            return View(loginRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during login (Login)");
            ViewBag.ErrorMessage = "Authentication service is temporarily unavailable. Please try again later.";
            ViewBag.Username = loginRequest.Username;
            return View(loginRequest);
        }
    }
    // SSO快捷登入 ()
    [HttpGet("WindowsSso")]
    [AllowAnonymous]
    public async Task<IActionResult> WindowsSso(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        try
        {
            // 嘗試從目前請求的 HTTP Header 讀取 Windows 驗證資訊 (Negotiate Scheme)
            var result = await HttpContext.AuthenticateAsync(NegotiateDefaults.AuthenticationScheme);

            // 身分挑戰交握（401 Challenge 握手）
            if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
            {
                return Challenge(NegotiateDefaults.AuthenticationScheme);
            }
            
            // 提取並清洗帳號名稱
            string cleanUsername  = CleanDomainUsername(result.Principal.Identity.Name);

            // 帳號有效性防禦檢查
            if (string.IsNullOrEmpty(cleanUsername))
            {
                ViewBag.ErrorMessage = "Unable to retrieve Windows identity from the domain.";
                return View("Login", new LoginRequest { returnUrl = returnUrl });
            }

            // 查詢 AD 群組與資料庫權限
            var permissions = await _authService.GetPermissionsByUsernameAsync(cleanUsername);
            // 建立cookie 驗證
            await SignInUserAsync(cleanUsername, permissions);

            return RedirectToLocal(returnUrl);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "LDAP/AD server is unavailable (WindowsSso)");
            ViewBag.ErrorMessage = "SSO authentication failed: Unable to connect to domain controller (LDAP Server Unavailable). Please contact network administrator.";
            return View("Login", new LoginRequest { returnUrl = returnUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during Windows SSO authentication");
            ViewBag.ErrorMessage = $"Error during SSO login: {ex.Message}";
            return View("Login", new LoginRequest { returnUrl = returnUrl });
        }
    }

    // 登出 cookie 驗證
    [HttpGet("Logout")]
    [HttpPost("Logout")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Cookie 驗證登出
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // 無足夠權限瀏覽
    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return Content("403 Forbidden: You are authenticated, but not authorized to access this resource. Please contact your system administrator to assign the required AD group.");
    }

    // 建立 Cookie 登入工作階段
    private async Task SignInUserAsync(string username, List<string> permissions)
    {
        // ClaimsPrincipal NetCore 身分創建
        var principal = _authService.CreatePrincipal(username, permissions);
        // 依上述身分創建建立 cookie 身分驗證
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    // 安全的網址重新導向
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrl.Contains("/Auth/Login", StringComparison.OrdinalIgnoreCase))
        {
            return Redirect(returnUrl);
        }

        return Redirect(DefaultRedirectUrl);
    }
    
    // 帳號名稱清洗過濾
    private static string CleanDomainUsername(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
        return rawName.Contains('\\') ? rawName.Split('\\')[1] : rawName;
    }
}
