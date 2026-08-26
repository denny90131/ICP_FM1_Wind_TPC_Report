using Microsoft.AspNetCore.Mvc;
using System.Text;
using tai_wind_integration.Modle.SystemConfig;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration; // 新增此行

[ApiController]
[Route("api/[controller]")]
public class SystemConfigController : ControllerBase
{
    private readonly ITokenManager _tokenManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration; // 新增 IConfiguration 依賴
    private readonly IBackgroundTaskStatusService _backgroundTaskStatusService;

    public SystemConfigController(IWebHostEnvironment webHostEnvironment, ITokenManager tokenManager, IConfiguration configuration, IBackgroundTaskStatusService backgroundTaskStatusService) // 注入
    {
        _tokenManager = tokenManager;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration; // 賦值
        _backgroundTaskStatusService = backgroundTaskStatusService;
    }

    /// <summary>
    /// 取得目前的 Token 狀態
    /// </summary>
    [HttpGet("token")]
    public IActionResult GetToken()
    {
        var token = _tokenManager.GetToken();
        return Ok(new { token });
    }

    /// <summary>
    /// 熱重載：動態更新記憶體中的 API Token
    /// </summary>
    [HttpPost("token")]
    public IActionResult UpdateToken([FromBody] TokenUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Token))
        {
            return BadRequest(new { code = 400, msg = "Token 不可為空" });
        }

        _tokenManager.SetToken(request.Token);

        return Ok(new { code = 200, msg = "Token 已成功熱重載更新！" });
    }

    /// <summary>
    /// 取得 API 服務設定 (Domain)
    /// </summary>
    [HttpGet("apiSettings")]
    public IActionResult GetApiSettings()
    {
        var apiDomain = _configuration["conf_API:Domain"];
        return Ok(new { apiDomain });
    }

    /// <summary>
    /// 取得 SFTP 服務設定 (Host, Port, Username, Password)
    /// </summary>
    [HttpGet("sftpSettings")]
    public IActionResult GetSftpSettings()
    {
        var sftpHost = _configuration["conf_SFTP:Host"];
        var sftpPort = _configuration.GetValue<int>("conf_SFTP:Port");
        var sftpUsername = _configuration["conf_SFTP:Username"];
        // 注意：將密碼直接暴露在前端（即使是唯讀）存在安全風險。
        // 在生產環境中應謹慎考慮此做法。
        var sftpPassword = _configuration["conf_SFTP:Password"];

        return Ok(new
        {
            sftpHost,
            sftpPort,
            sftpUsername,
            sftpPassword
        });
    }

    /// <summary>
    /// 取得所有背景服務的執行狀態
    /// </summary>
    [HttpGet("backgroundTaskStatus")]
    public IActionResult GetBackgroundTaskStatus()
    {
        var statuses = _backgroundTaskStatusService.GetAllStatuses();
        return Ok(statuses);
    }
    /// <summary>
    /// 取得Log
    /// </summary>
    [HttpGet("GetLogs")] 
    public async Task<IActionResult> GetLogs(string? searchTerm, string? logLevel, DateTime? date)
    {
        var logDirectory = Path.Combine(_webHostEnvironment.ContentRootPath, "logs");
        var logFiles = new List<string>();

        if (date.HasValue)
        {
            // If a specific date is provided, try to find that day's log file
            var specificLogFileName = $"log_{date.Value:yyyyMMdd}.txt";
            var specificLogFilePath = Path.Combine(logDirectory, specificLogFileName);
            if (System.IO.File.Exists(specificLogFilePath))
            {
                logFiles.Add(specificLogFilePath);
            }
        }
        else
        {
            // If no specific date, get all log files in the directory
            if (System.IO.Directory.Exists(logDirectory))
            {
                logFiles.AddRange(System.IO.Directory.GetFiles(logDirectory, "log_*.txt"));
            }
        }

        if (!logFiles.Any())
        {
            return Ok(new List<string> { "沒有找到日誌檔案。" });
        }

        var filteredLogEntries = new List<string>();
        foreach (var filePath in logFiles.OrderBy(f => f)) // Order by filename (date)
        {
            // 使用 FileStream 搭配 FileShare.ReadWrite 避免檔案被其他行程鎖定而報錯
            var lines = new List<string>();
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            filteredLogEntries.AddRange(lines.Where(line =>
                (string.IsNullOrWhiteSpace(searchTerm) || line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(logLevel) || Regex.IsMatch(line, $@".*\[{logLevel.ToUpper()}\].*", RegexOptions.IgnoreCase))
            ));
        }

        return Ok(filteredLogEntries);
    }

}