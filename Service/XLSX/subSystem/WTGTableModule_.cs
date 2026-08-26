using tai_wind_integration.Modle.Xlsx;

public class WtgTableModule_ : ISubsystemModule
{
    // 子系統的唯一識別碼，必須與前端傳入或對應的 ID 一致
    public string ModuleId => "Substation_Data";

    // 子系統的顯示名稱
    public string ModuleName => "風機數據表處理模組222";

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
                    Message = "沒有找到符合條件的檔案可供處理。" 
                };
            }

            // 2. 逐一處理這些檔案
            foreach (var filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                
                // 模擬讀取或處理檔案的邏輯
                // 這裡你可以使用你專案中的 tai_wind_integration.Modle.Xlsx 相關工具來讀取 Excel / CSV
                Console.WriteLine($"正在處理檔案: {fileName}");
                
                // 假設需要非同步模擬
                await Task.Delay(100); 
            }

            return new ExecutionResult 
            { 
                IsSuccess = true, 
                Message = $"成功完成處理，共處理了 {files.Count} 個檔案。" 
            };
        }

        return new ExecutionResult 
        { 
            IsSuccess = false, 
            Message = "未收到有效的檔案清單參數 (Filtered_Files)。" 
        };
    }
}