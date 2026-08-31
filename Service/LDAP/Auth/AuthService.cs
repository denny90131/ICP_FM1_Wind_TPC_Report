using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly string _domain;
    private readonly string _ldapPath;
    private readonly string? _serviceUser;
    private readonly string? _servicePassword;

    public AuthService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        // 1. 直接讀取 appsettings.json 現有的 ActiveDirectory:LdapPath
        _ldapPath = configuration["ActiveDirectory:LdapPath"] ?? "LDAP://fm1.local";
        
        // 2. 從 "LDAP://fm1.local" 自動解析出 domain 名稱 "fm1.local"
        _domain = _ldapPath.Replace("LDAP://", "", StringComparison.OrdinalIgnoreCase).TrimEnd('/');
    }

    // 登入 AD 之用戶身分 (帳密手動輸入)
    public async Task<LoginResponse> LoginWithAdAsync(string username, string password)
    {
        try
        {
            using (var context = new PrincipalContext(ContextType.Domain, _domain))
            {
                bool isValid = context.ValidateCredentials(username, password);
                if (!isValid)
                {
                    return new LoginResponse
                    {
                        LoginStatus = false,
                        UserName = username,
                        ErrorMessage = "帳號或密碼錯誤。"
                    };
                }
            }

            var permissions = await GetPermissionsByUsernameAsync(username);
            return new LoginResponse
            {
                LoginStatus = true,
                UserName = username,
                Permission = permissions,
                ErrorMessage = string.Empty
            };
        }
        catch (Exception ex)
        {
            return new LoginResponse
            {
                LoginStatus = false,
                UserName = username,
                ErrorMessage = $"AD 連線驗證失敗: {ex.Message}"
            };
        }
    }

    // 取得 AD 用戶之內部使用權限 (相容手動登入與 SSO)
    public async Task<List<string>> GetPermissionsByUsernameAsync(string username)
    {
        var userGroups = new List<string>();
        
        // 3. 建立 AD 網域查詢
        using (DirectoryEntry entry = new DirectoryEntry(_ldapPath))

        using (DirectorySearcher searcher = new DirectorySearcher(entry))
        {
            
            // 向 AD 搜尋該使用者的所屬群組 (LDAP 查詢)
            searcher.Filter = $"(&(objectCategory=person)(objectClass=user)(sAMAccountName={username}))";
            searcher.PropertiesToLoad.Add("memberOf");

            // 解析使用所在群組，並簡化
            SearchResult? result = searcher.FindOne();
            if (result != null && result.Properties.Contains("memberOf"))
            {
                foreach (var groupDn in result.Properties["memberOf"])
                {
                    string groupDnStr = groupDn?.ToString() ?? string.Empty;
                    // 解析 DN 中的 CN (例如 "CN=Admins,OU=Groups,DC=fm1,DC=local" -> "Admins")
                    string groupName = ExtractCn(groupDnStr);
                    if (!string.IsNullOrEmpty(groupName))
                    {
                        userGroups.Add(groupName);
                    }
                }
            }
        }

        // 若使用者不再任何群組，則返還空白權限
        if (!userGroups.Any())
        {
            return new List<string>();
        }

        // 依據所在之群組 比對 系統資料庫權限
        var permissions = await _db.Roles
            .Where(r => userGroups.Contains(r.AdGroupName))
            .SelectMany(r => r.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .ToListAsync();

        //返還權限列表
        return permissions;
    }
    
    // 建立 ClaimsPrincipal 物件 (Cliams)
    public ClaimsPrincipal CreatePrincipal(string username, List<string> permissions)
    {
        // 建立 Cliams 列表，放入基本帳號資料
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, username)
        };
        
        // AD/資料庫查到的所有權限字串，逐筆以 "Permission" 為標籤加入清單
        foreach (var perm in permissions)
        {
            claims.Add(new Claim("Permission", perm));
        }

        // Claims 建立一張身分識別證，並指定驗證方案為 Cookies
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        // 將 Identity 包裝成代表整個使用者的主體物件（Principal）並回傳
        return new ClaimsPrincipal(identity);
    }

    // 從 LDAP DN 解析 Common Name (CN) - 元件類別
    private static string ExtractCn(string dn)
    {
        if (string.IsNullOrWhiteSpace(dn)) return string.Empty;

        var parts = dn.Split(',');
        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            if (trimmed.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(3);
            }
        }
        return dn;
    }
}