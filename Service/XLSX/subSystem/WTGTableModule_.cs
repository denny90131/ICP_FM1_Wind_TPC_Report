using tai_wind_integration.Modle.Xlsx;

public class WtgTableModule_ : ISubsystemModule
{
    // 子系統的唯一識別碼，必須與前端傳入或對應的 ID 一致
    public string ModuleId => "Substation_Data";

    // 子系統的顯示名稱
    public string ModuleName => "Custom_bk";

    public async Task<ExecutionResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        // 1. 從 parameters 中取出 SubsystemOrchestrator 幫忙過濾好的檔案清單
        if (parameters.TryGetValue("Filtered_Files", out var filesObj) && filesObj is List<string> files)
        {
            if (files.Count == 0)
            {
                return new ExecutionResult 
                { 
                    IsSuccess = true, 
                    Message = "No matching files found to process." 
                };
            }

            // 2. 逐一處理這些檔案
            foreach (var filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                
                // 模擬讀取或處理檔案的邏輯
                // 這裡你可以使用你專案中的 tai_wind_integration.Modle.Xlsx 相關工具來讀取 Excel / CSV
                Console.WriteLine($"Processing file: {fileName}");
                
                // 假設需要非同步模擬
                await Task.Delay(100); 
            }

            return new ExecutionResult 
            { 
                IsSuccess = true, 
                Message = $"Successfully processed {files.Count} file(s)." 
            };
        }

        return new ExecutionResult 
        { 
            IsSuccess = false, 
            Message = "Invalid or missing file list parameter (Filtered_Files)." 
        };
    }
}
