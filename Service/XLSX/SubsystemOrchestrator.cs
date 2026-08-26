using Microsoft.Extensions.Configuration; // 需要引用這個
using Microsoft.Extensions.Logging;
using tai_wind_integration.Modle.Xlsx;

public class SubsystemOrchestrator
{
    private readonly IEnumerable<ISubsystemModule> _modules;
    private readonly ILogger<SubsystemOrchestrator> _logger;
    private readonly IConfiguration _configuration;

    // 1. 在建構子多注入 IConfiguration 
    public SubsystemOrchestrator(
        IEnumerable<ISubsystemModule> modules, 
        ILogger<SubsystemOrchestrator> logger,
        IConfiguration configuration) // 自動取得 appsettings.json
    {
        _modules = modules;
        _logger = logger;
        _configuration = configuration;

        // 2. 讓它在程式啟動建立 Orchestrator 時，自動執行一次
        // 使用 Task.Run 避免在 DI 建構子中直接 async/await 造成死結
        Task.Run(async () => await AutoRunFromConfigAsync());
    }

    // 程式啟動時的自動多組執行
    private async Task AutoRunFromConfigAsync()
    {
        try
        {
            await Task.Delay(1000);
            _logger.LogInformation("【自動執行】準備從設定檔讀取多組子系統設定...");
            
            var results = await RunAllFromConfigAsync();
            
            foreach (var res in results)
            {
                _logger.LogInformation("模組 {ModuleId} 自動執行結果: Success={IsSuccess}, Message={Message}", 
                    res.Key, res.Value.IsSuccess, res.Value.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "【自動執行】初始化多組設定時發生例外狀況");
        }
    }

    // 取得目前系統內有哪些子系統（維持原樣）
    public IEnumerable<object> GetAvailableModules()
    {
       // 1. 從 appsettings.json 讀取所有設定好的模組清單
        var configSections = _configuration.GetSection("conf_review:Modules").Get<List<ModuleConfigItem>>();
        
        if (configSections == null || !configSections.Any())
        {
            return Enumerable.Empty<object>(); // 如果設定檔沒寫，直接回傳空集合
        }

        // 2. 取出設定檔中有出現的 ModuleId 集合 (忽略大小寫)
        var configuredModuleIds = configSections
            .Select(c => c.ModuleId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 3. 過濾 _modules：只留下存在於設定檔清單中的模組
        return _modules
            .Where(m => configuredModuleIds.Contains(m.ModuleId))
            .Select(m => new { m.ModuleId, m.ModuleName });
    }

    /// <summary>
    /// 【核心功能】讀取設定檔中所有設定，自動分配並執行對應的子系統
    /// </summary>
    public async Task<Dictionary<string, ExecutionResult>> RunAllFromConfigAsync()
    {
        var summaryResults = new Dictionary<string, ExecutionResult>();

        // 綁定設定檔中的清單
        var configSections = _configuration.GetSection("conf_review:Modules").Get<List<ModuleConfigItem>>();

        if (configSections == null || !configSections.Any())
        {
            _logger.LogWarning("在設定檔中找不到任何 conf_review:Modules 設定");
            return summaryResults;
        }

        foreach (var config in configSections)
        {
            if (string.IsNullOrWhiteSpace(config.ModuleId)) continue;

            _logger.LogInformation("【設定檔自動分配】正在派發至子系統: {ModuleId}, 路徑: {Path}", config.ModuleId, config.Folder_Path);

            var parameters = new Dictionary<string, object>
            {
                { "Folder_Path", config.Folder_Path ?? "" },
                { "File_Type", config.File_Type ?? "*.csv" }
            };

            // 呼叫共用的執行方法，自動對應到相對應的子系統
            var result = await RunModuleAsync(config.ModuleId, parameters);
            summaryResults[config.ModuleId] = result;
        }

        return summaryResults;
    }

    // 執行指定的子系統（維持原樣）
    public async Task<ExecutionResult> RunModuleAsync(string moduleId, Dictionary<string, object> parameters)
    {
        var targetModule = _modules.FirstOrDefault(m => m.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));
        
        if (targetModule == null)
        {
            _logger.LogWarning("嘗試執行不存在的子系統 ID: {ModuleId}", moduleId);
            return new ExecutionResult { IsSuccess = false, Message = $"找不到指定的子系統: {moduleId}" };
        }

        try
        {
            string folderPath = parameters.TryGetValue("Folder_Path", out var pathObj) ? pathObj?.ToString() ?? "" : "";
            string fileType = parameters.TryGetValue("File_Type", out var typeObj) ? typeObj?.ToString() ?? "*.csv" : "*.csv";

            List<string> filteredFiles = new List<string>();
            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                filteredFiles = Directory.GetFiles(folderPath, fileType).ToList();
                _logger.LogInformation("在路徑 {FolderPath} 下找到 {Count} 個符合 {FileType} 的檔案", folderPath, filteredFiles.Count, fileType);
            }
            else
            {
                _logger.LogWarning("無效的資料夾路徑: {FolderPath}", folderPath);
            }

            parameters["Filtered_Files"] = filteredFiles;

            _logger.LogInformation("開始執行子系統: {ModuleName}", targetModule.ModuleName);
            return await targetModule.ExecuteAsync(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "執行子系統 {ModuleName} 時發生未預期的錯誤", targetModule.ModuleName);
            return new ExecutionResult { IsSuccess = false, Message = $"執行錯誤: {ex.Message}" };
        }
    }

    /// <string, object> 或你可以建立專屬的DTO來承載回傳結果
    public async Task<ExecutionResult> RunModuleByAbsolutePathAsync(string moduleId, string absoluteFilePath)
    {
        if (string.IsNullOrWhiteSpace(absoluteFilePath) || !File.Exists(absoluteFilePath))
        {
            _logger.LogWarning("提供的絕對路徑無效或檔案不存在: {Path}", absoluteFilePath);
            return new ExecutionResult 
            { 
                IsSuccess = false, 
                Message = $"找不到指定的檔案或路徑無效: {absoluteFilePath}" 
            };
        }

        // 取得檔案所在的資料夾與副檔名，組裝成原本子系統看得懂的參數
        string folderPath = Path.GetDirectoryName(absoluteFilePath) ?? "";
        string fileName = Path.GetFileName(absoluteFilePath);
        string fileExtension = Path.GetExtension(absoluteFilePath);

        var parameters = new Dictionary<string, object>
        {
            { "Folder_Path", folderPath },
            { "File_Type", $"*{fileExtension}" }, // 例如 *.csv
            { "Target_File_Name", fileName }       // 額外指定單一檔名供子系統識別
        };

        _logger.LogInformation("【母系統】準備以絕對路徑執行子系統: {ModuleId}, 檔案: {FilePath}", moduleId, absoluteFilePath);

        // 呼叫原本的執行邏輯
        return await RunModuleAsync(moduleId, parameters);
    }
    /// <summary>
    /// 絕對路徑讀取csv資料
    /// </summary>
    /// <param name="absoluteFilePath"></param>
    /// <returns></returns> <summary>
    public async Task<List<Dictionary<string, string>>> ReadCsvDataAsync(string absoluteFilePath)
    {
        var resultList = new List<Dictionary<string, string>>();

        if (!File.Exists(absoluteFilePath))
        {
            _logger.LogWarning("欲讀取的 CSV 檔案不存在: {Path}", absoluteFilePath);
            return resultList;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(absoluteFilePath);
            if (lines.Length == 0) return resultList;

            // 假設第一行為標題 (Header)
            var headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                var dataRow = lines[i].Split(',');
                var rowDict = new Dictionary<string, string>();

                for (int j = 0; j < headers.Length && j < dataRow.Length; j++)
                {
                    rowDict[headers[j].Trim()] = dataRow[j].Trim();
                }
                resultList.Add(rowDict);
            }

            _logger.LogInformation("成功讀取 CSV 檔案，共解析 {Count} 筆資料", resultList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析 CSV 檔案時發生錯誤: {Path}", absoluteFilePath);
        }

        return resultList;
    }
}