using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using tai_wind_integration.Modle.Xlsx;

[ApiController]
[Route("api/[controller]")]
public class ExcelReaderController : ControllerBase
{
    private readonly SubsystemOrchestrator _orchestrator;
    private readonly IConfiguration _configuration;

    public ExcelReaderController(SubsystemOrchestrator orchestrator, IConfiguration configuration)
    {
        _orchestrator = orchestrator;
        _configuration = configuration;
    }

    // 批次執行設定檔中所有的子系統設定
    [HttpPost("run-all-by-config")]
    public async Task<IActionResult> RunAllByConfig()
    {
        var results = await _orchestrator.RunAllFromConfigAsync();
        return Ok(results);
    }
    
    // 方式 A：直接讀取 appsettings.json 的路徑來執行
    [HttpPost("run-by-config")]
    public async Task<IActionResult> RunByConfig()
    {
        // 從 appsettings.json 的 conf_review:Modules 中找到 WTG_Table 的設定
        var moduleConfig = _configuration.GetSection("conf_review:Modules")
            .Get<List<ModuleConfigItem>>()?
            .FirstOrDefault(m => m.ModuleId == "WTG_Table");

        if (moduleConfig == null)
        {
            return NotFound("在 appsettings.json 中找不到 WTG_Table 的設定。");
        }

        var parameters = new Dictionary<string, object>
        {
            { "Folder_Path", moduleConfig.Folder_Path ?? "" },
            { "File_Type", moduleConfig.File_Type ?? "*.csv" }
        };

        // 執行指定的子系統
        var result = await _orchestrator.RunModuleAsync("WTG_Table", parameters);

        return Ok(result);
    }

    // ================= 新增：取得目前所有可用的子系統清單 =================
    [HttpGet("modules")]
    public IActionResult GetAvailableModules()
    {
        var modules = _orchestrator.GetAvailableModules();
        return Ok(modules);
    }

    // ================= 新增：根據 ModuleId 取得對應的檔案清單 =================
    [HttpGet("files/{moduleId}")]
    public IActionResult GetFilesForModule(string moduleId, [FromQuery] string? path = null)
    {
        // 從設定檔讀取該模組的路徑
        var moduleConfig = _configuration.GetSection("conf_review:Modules")
            .Get<List<ModuleConfigItem>>()?
            .FirstOrDefault(m => m.ModuleId.Equals(moduleId, StringComparison.OrdinalIgnoreCase));

        // 找不到設定或路徑為空，回傳空清單
        if (moduleConfig == null || string.IsNullOrWhiteSpace(moduleConfig.Folder_Path))
        {
            return Ok(new { CurrentPath = "/", Items = new List<object>() });
        }

        string basePath = moduleConfig.Folder_Path;
        // 如果沒有傳入 path，就使用基礎路徑；否則，組合路徑
        string currentPath = string.IsNullOrEmpty(path) ? basePath : Path.GetFullPath(Path.Combine(basePath, path));

        // 安全性檢查：確保請求的路徑在設定的基礎路徑之內，防止任意路徑存取
        if (!currentPath.StartsWith(Path.GetFullPath(basePath)))
        {
            return BadRequest(new { Message = "Access denied to the specified path." });
        }

        if (!Directory.Exists(currentPath))
        {
            return Ok(new { CurrentPath = path, Items = new List<object>() });
        }

        var items = new List<object>();

        // 1. 取得所有子資料夾
        foreach (var dir in Directory.GetDirectories(currentPath))
        {
            items.Add(new
            {
                Name = Path.GetFileName(dir),
                Path = Path.GetRelativePath(basePath, dir).Replace('\\', '/'), // 使用相對路徑，並將反斜線換成正斜線
                IsDirectory = true
            });
        }

        // 2. 取得所有符合條件的檔案
        var fileType = moduleConfig.File_Type ?? "*.csv"; // 預設為 .csv
        foreach (var file in Directory.GetFiles(currentPath, fileType))
        {
            items.Add(new
            {
                Name = Path.GetFileName(file),
                Path = file, // 檔案直接回傳完整路徑
                IsDirectory = false
            });
        }

        // 回傳目前路徑和其下的項目列表
        return Ok(new { CurrentPath = string.IsNullOrEmpty(path) ? "/" : path, Items = items });
    }

    // [新增] 方式 C：執行指定子系統，並傳入絕對路徑
    [HttpPost("execute-by-file")]
    public async Task<IActionResult> ExecuteByFile([FromBody] FilePathRequest request)
    {
        // 呼叫你在 Orchestrator 新增的 RunModuleByAbsolutePathAsync
        var result = await _orchestrator.RunModuleByAbsolutePathAsync(request.ModuleId, request.FilePath);
        
        if (!result.IsSuccess) return BadRequest(result);

        // 這裡我們直接把讀取到的資料回傳給前端
        var data = await _orchestrator.ReadCsvDataAsync(request.FilePath);
        result.Data = data; // 將資料塞回 result 物件

        return Ok(result);
    }
}

// 用於方式 B 的請求資料模型 (此處未使用，但保留)
public class ExcelRunRequest
{
    public string FolderPath { get; set; } = string.Empty;
    public string FileType { get; set; } = "*.xlsx";
}