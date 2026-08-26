using Microsoft.Extensions.Options;
using Renci.SshNet;
using tai_wind_integration.Modle.SFTP;

public class SftpService : ISftpService
{
    private readonly conf_SFTP _settings;
    private readonly ILogger<SftpService> _logger; // 宣告 Logger

    // 初次載入時，透過 IOptions 抓取 appsettings 相關設定
    public SftpService(IOptions<conf_SFTP> options, ILogger<SftpService> logger)
    {    
        _settings = options.Value;
        _logger = logger;
        // VaildateSettings(_settings);
    }

    /// <summary>
    /// 檢查設定檔填寫狀況 (排查用)
    /// </summary>
    private void VaildateSettings(conf_SFTP conf)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("【SFTP Service】initial Check and Load Configuration...");
        _logger.LogInformation($"Host     : {(conf.Host ?? "Non")}");
        _logger.LogInformation($"Port     : {conf.Port}");
        _logger.LogInformation($"UserName : {(conf.Username ?? "Non")}");
        
        if (string.IsNullOrWhiteSpace(conf.Host) || 
            string.IsNullOrWhiteSpace(conf.Username) || 
            string.IsNullOrWhiteSpace(conf.Password))
        {
            _logger.LogError("【SFTP Service】SFTP Configuration incomplete (Host、Username、Password is Non)");
        }
        else
        {
            _logger.LogInformation("【SFTP Service】SFTP Configuraion is readly");
        }
        _logger.LogInformation("========================================");
    }

    /// <summary>
    /// 驗證 SFTP 通訊
    /// </summary>
    public void VerifyCommunication()
    {
        _logger.LogInformation($"【SFTP Service】Communcation Verify...");
        try
        {
            using var client = new SftpClient(_settings.Host, _settings.Port, _settings.Username, _settings.Password);
            
            // 連線並捕捉網路或驗證錯誤
            client.Connect();
            
            client.Disconnect();
            _logger.LogInformation($"【SFTP Service】Communcation Verify Success !!!!");
        }
        catch (Exception ex)
        {
            // 在 Service 內部進行除錯紀錄或拋出具體錯誤
            _logger.LogError($"【SFTP Service】Communcation Verify Error ，Expection Error:  {ex.Message}");
            throw new InvalidOperationException($"SFTP Upload Failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// SFTP 檔案上傳
    /// </summary>
    public void UploadFile(string localPath, string remotePath)
    {
       // 1. 邏輯判斷：檢查本機檔案是否存在
        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException($"Local file to upload not found: {localPath}");
        }

        _logger.LogInformation($"【SFTP Service】Local path: {localPath} -> Remote path: {remotePath}");
        try
        {
            using var client = new SftpClient(_settings.Host, _settings.Port, _settings.Username, _settings.Password);
            
            // 連線並捕捉網路或驗證錯誤
            client.Connect();
            
            using var fileStream = File.OpenRead(localPath);
            client.UploadFile(fileStream, remotePath);
            
            client.Disconnect();
            _logger.LogInformation($"【SFTP Service】File successfully transferred: {remotePath}");
        }
        catch (Exception ex)
        {
            // 在 Service 內部進行除錯紀錄或拋出具體錯誤
            _logger.LogError($"【SFTP Service】 Error: {ex.Message}");
            throw new InvalidOperationException($"SFTP upload failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// SFTP 檔案下載
    /// </summary>
    public void DownloadFile(string remotePath, string localPath)
    {
        _logger.LogInformation($"【SFTP Service】 Remote path: {remotePath} -> Local path: {localPath}");

        try
        {
            using var client = new SftpClient(_settings.Host, _settings.Port, _settings.Username, _settings.Password);
            client.Connect();

            using var fileStream = File.OpenWrite(localPath);
            client.DownloadFile(remotePath, fileStream);

            client.Disconnect();
            _logger.LogInformation($"【SFTP Service】Download Succeeded");
        }
        catch (Exception ex)
        {
            _logger.LogError($"【SFTP Service】 Error: {ex.Message}");
            throw new InvalidOperationException($"SFTP download failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 對外公開：取得遠端 SFTP 目錄結構
    /// </summary>
    public IEnumerable<SftpItemDto> GetDirectoryStructure(string startPath = "/")
    {
        _logger.LogInformation($"【SFTP Service】 Loading Remote Path..: {startPath}");

        try
        {
            using var client = new SftpClient(_settings.Host, _settings.Port, _settings.Username, _settings.Password);
            client.Connect();

            var items = new List<SftpItemDto>();
            
            // 定義要徹底略過的系統與敏感資料夾（不論大小寫）
            var ignoredFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "$Recycle.Bin",
                "System Volume Information",
                "Documents and Settings",
                "Windows",
                "Program Files",
                "Program Files (x86)",
                "ProgramData",
                "Recovery",
                "Config.Msi",
                "MSOCache",
                // 使用者資料夾底下的 Windows 舊版符號連結/隱藏捷徑
                "Application Data",
                "Local Settings",
                "My Documents",
                "NetHood",
                "PrintHood",
                "Recent",
                "SendTo",
                "Templates",
                "Cookies",
                "「開始」功能表",
                "Start Menu"
            };

            var files = client.ListDirectory(startPath);

            foreach (var file in files)
            {
                if (file.Name == "." || file.Name == "..")
                    continue;

                if (file.IsDirectory && ignoredFolders.Contains(file.Name))
                {
                    _logger.LogTrace($"【SFTP Service】 Skipped system directory: {startPath}/{file.Name}");
                    continue;
                }

                items.Add(new SftpItemDto
                {
                    Name = file.Name,
                    Path = $"{startPath.TrimEnd('/')}/{file.Name}",
                    IsDirectory = file.IsDirectory,
                    Size = file.IsDirectory ? 0 : file.Length,
                    Children = new List<SftpItemDto>() // 永遠是空的，因為我們只讀取一層
                });
            }

            client.Disconnect();
            _logger.LogInformation("【SFTP Service】 Reading completed successfully");
            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "【SFTP Service】 Failed to read path {Path}: {Message}", startPath, ex.Message);
            throw new InvalidOperationException($"SFTP 取得目錄結構失敗: {ex.Message}", ex);
        }
    }
}