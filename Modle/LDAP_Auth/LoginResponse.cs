public class LoginResponse
{
    // 登入狀態
    public bool LoginStatus { get; set; } = false;

    // 使用者登入權限清單
    public List<string> Permission {get;set;} = new List<string>();
    
    // 使用者名稱
    public string UserName{ get; set; } = string.Empty;

    // 錯誤訊息
    public string? ErrorMessage { get; set; }
}