using System.Security.Claims;

public interface IAuthService
{
    // 方案 A: 帳密登入驗證
    Task<LoginResponse> LoginWithAdAsync(string username, string password);

    // 方案 B: Windows 整合驗證 / SSO 登入
    Task<List<string>> GetPermissionsByUsernameAsync(string username);
    // 創建 Cookie
    ClaimsPrincipal CreatePrincipal(string username, List<string> permissions);
}