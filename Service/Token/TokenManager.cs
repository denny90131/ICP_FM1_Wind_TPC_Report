public class TokenManager : ITokenManager
{
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly string _tokenFilePath = "token.json"; // 獨立儲存 Token 的檔案
    private readonly ILogger<TokenManager> _logger;
    private string? _token;

    public TokenManager(IConfiguration configuration, ILogger<TokenManager> logger)
    {
        _logger = logger;

        // 1. 優先檢查是否有獨立儲存的 Token 檔案
        if (File.Exists(_tokenFilePath))
        {
            try
            {
                var savedToken = File.ReadAllText(_tokenFilePath).Trim();
                if (!string.IsNullOrEmpty(savedToken))
                {
                    _token = savedToken;
                    return;
                }
            }
            catch
            {
                // 讀取失敗時略過，改從設定檔讀取
            }
        }

        // 2. 如果沒有獨立檔案，才從 appsettings.json 載入初始 Token
        _token = configuration["conf_API:Token"];
    }

    public string? GetToken()
    {
        _logger.LogInformation("【TokenManager】Loading Token...");
        _lock.EnterReadLock();
        
        try
        {
            _logger.LogInformation("【TokenManager】Recived Token");
            return _token;
        }
        catch(Exception ex)
        {
            _logger.LogError($"【TokenManager】failed Fetch {ex.Message}");
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void SetToken(string newToken)
    {
        _logger.LogInformation("【TokenManager】Configuration Token...");
        _lock.EnterWriteLock();
        try
        {
            _token = newToken;

            // 同步寫入檔案，確保重啟後讀得到
            File.WriteAllText(_tokenFilePath, newToken);
            _logger.LogInformation("【TokenManager】Token Configurate Complete...");
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}