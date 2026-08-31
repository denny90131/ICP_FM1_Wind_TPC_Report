public class LoginRequest
{
    // 登入使用者帳號
    public string Username { get; set; } = string.Empty;

    // 登入使用者密碼
    public string Password {get;set;} = string.Empty;
    
    // 轉向目標路徑
    public string? returnUrl{ get; set; } = null;
}