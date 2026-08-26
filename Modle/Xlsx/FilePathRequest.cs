// 統一的請求模型
public class FilePathRequest
{
    public string ModuleId { get; set; } = "WTG_Table"; // 預設值
    public string FilePath { get; set; } = string.Empty; // 這裡丟入絕對路徑
}