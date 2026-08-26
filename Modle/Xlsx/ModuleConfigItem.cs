// 用於對應設定檔的對應類別
public class ModuleConfigItem
{
    public string ModuleId { get; set; } = string.Empty;
    public string Folder_Path { get; set; } = string.Empty;
    public string File_Type { get; set; } = "*.csv";
}