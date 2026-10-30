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
            _logger.LogInformation("[Auto-Run] Preparing to load subsystem configurations from config file...");
            
            var results = await RunAllFromConfigAsync();
            
            foreach (var res in results)
            {
                _logger.LogInformation("Module {ModuleId} auto-run result: Success={IsSuccess}, Message={Message}", 
                    res.Key, res.Value.IsSuccess, res.Value.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Auto-Run] An unexpected exception occurred while initializing multi-module configuration.");
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
            _logger.LogWarning("No conf_review:Modules configurations found in appsettings.");
            return summaryResults;
        }

        foreach (var config in configSections)
        {
            if (string.IsNullOrWhiteSpace(config.ModuleId)) continue;

            _logger.LogInformation("[Config Auto-Dispatch] Dispatching to subsystem: {ModuleId}, Path: {Path}", config.ModuleId, config.Folder_Path);

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
            _logger.LogWarning("Attempted to execute non-existent subsystem ID: {ModuleId}", moduleId);
            return new ExecutionResult { IsSuccess = false, Message = $"Subsystem not found: {moduleId}" };
        }

        try
        {
            string folderPath = parameters.TryGetValue("Folder_Path", out var pathObj) ? pathObj?.ToString() ?? "" : "";
            string fileType = parameters.TryGetValue("File_Type", out var typeObj) ? typeObj?.ToString() ?? "*.csv" : "*.csv";

            List<string> filteredFiles = new List<string>();
            if (!string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath))
            {
                filteredFiles = Directory.GetFiles(folderPath, fileType).ToList();
                _logger.LogInformation("Found {Count} file(s) matching {FileType} in path {FolderPath}", filteredFiles.Count, fileType, folderPath);
            }
            else
            {
                _logger.LogWarning("Invalid folder path: {FolderPath}", folderPath);
            }

            parameters["Filtered_Files"] = filteredFiles;

            _logger.LogInformation("Starting subsystem: {ModuleName}", targetModule.ModuleName);
            return await targetModule.ExecuteAsync(parameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred while executing subsystem: {ModuleName}", targetModule.ModuleName);
            return new ExecutionResult { IsSuccess = false, Message = $"Execution error: {ex.Message}" };
        }
    }

    /// <string, object> 或你可以建立專屬的DTO來承載回傳結果
    public async Task<ExecutionResult> RunModuleByAbsolutePathAsync(string moduleId, string absoluteFilePath)
    {
        if (string.IsNullOrWhiteSpace(absoluteFilePath) || !File.Exists(absoluteFilePath))
        {
            _logger.LogWarning("The provided absolute path is invalid or the file does not exist: {Path}", absoluteFilePath);
            return new ExecutionResult 
            { 
                IsSuccess = false, 
                Message = $"Specified file not found or path is invalid: {absoluteFilePath}" 
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

        _logger.LogInformation("[Host System] Executing subsystem: {ModuleId} with absolute file path: {FilePath}", moduleId, absoluteFilePath);

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
            _logger.LogWarning("Target CSV file does not exist: {Path}", absoluteFilePath);
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

            _logger.LogInformation("Successfully read CSV file. Parsed {Count} rows.", resultList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while parsing CSV file: {Path}", absoluteFilePath);
        }

        return resultList;
    }
}
