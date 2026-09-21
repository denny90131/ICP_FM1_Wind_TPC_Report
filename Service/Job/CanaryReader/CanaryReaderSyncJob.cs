using System.Diagnostics;

public class CanaryReaderSyncJob : ICanaryReaderSyncJob
{
    private readonly ILogger<CanaryReaderSyncJob> _logger; // 宣告 Logger
    private readonly ICanaryService _canaryService;
    private readonly IConfiguration _configuration;

    // 初次載入時，透過 IOptions 抓取 appsettings 相關設定
    public CanaryReaderSyncJob(
        ILogger<CanaryReaderSyncJob> logger, 
        ICanaryService canaryService,
        IConfiguration configuration)
    {
        _logger = logger;
        _canaryService = canaryService;
        _configuration = configuration;
    }

    /// <summary>
    /// 執行主程序邏輯：抓取 33 台風機最新數據並轉換為 [內部名稱, 數值]
    /// </summary>
    /// <returns>回傳 Dictionary<內部名稱, 數值>，例如 ["WTG01_ActivePower"] = 1250.4</returns>
    public async Task<Dictionary<string, object?>> SyncTurbineDataAsync()
    {
        var sw = Stopwatch.StartNew();
        var turbineValues = new Dictionary<string, object?>();
        _logger.LogInformation("Canary Reader Sync Job 開始執行...");

        try
        {
            // 動態建立 Tag 對照表：Key 為 Canary SCADA Tag，Value 為內部自訂名稱
            // 例: ["FM1...WTG01.Grid.mea.ActivePower.P"] = "WTG01_ActivePower"
            Dictionary<string, string> tagToInternalKeyMap = GenerateTurbineTagMapping();
            // 設定檔查無任何點為，回傳空字典
            if (tagToInternalKeyMap.Count == 0)
            {
                _logger.LogWarning("CanaryTagMapping 查無任何 Tag 設定，作業中止。");
                return turbineValues;
            }

            // 2. 呼叫 Canary GetData2參數設定
            var request = new GetTagData2RequestDto
            {
                Tags = tagToInternalKeyMap.Keys.ToList(),
                IncludeQuality = true,
                UseTimeExtension = true
            };

            var response = await _canaryService.GetTagData2Async(request);

            if (!response.IsSuccess || response.Data?.Data == null)
            {
                _logger.LogError("Canary API 呼叫失敗: StatusCode={StatusCode}, Errors={Errors}", 
                    response.StatusCode, response.Errors);
                return turbineValues;
            }

            // 3. 轉換成 [內部系統名稱, 數值]
            foreach (var (canaryTag, points) in response.Data.Data)
            {
                // 從對照表反查內部名稱
                if (tagToInternalKeyMap.TryGetValue(canaryTag, out var internalName))
                {
                    var latestPoint = points?.LastOrDefault();
                    // 取出數值 (latestPoint.V)
                    turbineValues[internalName] = latestPoint?.V;
                }
            }

            // --- 4. 在此印出結果 ---
            _logger.LogInformation("=== 風機數據同步明細 (共 {Count} 筆) ===", turbineValues.Count);
            foreach (var item in turbineValues)
            {
                _logger.LogInformation("[Point] {InternalName}: {Value}", item.Key, item.Value ?? "null");
            }
            _logger.LogInformation("=========================================");

            sw.Stop();
            _logger.LogInformation("Canary 數據轉換完成，共解析 {Count} 個內部點位，耗時: {Elapsed} ms", 
                turbineValues.Count, sw.ElapsedMilliseconds);

            return turbineValues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Canary Reader Sync Job 執行時發生未預期錯誤。");
            return turbineValues;
        }
    }


    /// <summary>
    /// 解析 appsettings 中的 CanaryTagMapping，
    /// 產生 [Canary SCADA Tag -> 內部系統名稱] 的對應字典
    /// </summary>
    private Dictionary<string, string> GenerateTurbineTagMapping()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var mappingSection = _configuration.GetSection("CanaryTagMapping").GetChildren();

        foreach (var item in mappingSection)
        {
            string propertyName = item.Key;   // 例如: "ActivePower"
            string? template = item.Value;     // 樣板字串

            if (string.IsNullOrWhiteSpace(template)) continue;

            for (int i = 1; i <= 33; i++)
            {
                string wtgCode = $"WTG{i:D2}";         // WTG01 ~ WTG33
                string strCode = GetStringByWtgIndex(i); // 依編號算出對應的 WTG_StrA, WTG_StrB...

                // 解析 SCADA Tag (同時替換 String 與 WTG)
                string resolvedCanaryTag = template;

                // 替換 String
                if (resolvedCanaryTag.Contains("{STR}"))
                    resolvedCanaryTag = resolvedCanaryTag.Replace("{STR}", strCode);
                else
                    resolvedCanaryTag = resolvedCanaryTag.Replace("WTG_StrA", strCode);

                // 替換 WTG
                if (resolvedCanaryTag.Contains("{WTG}"))
                    resolvedCanaryTag = resolvedCanaryTag.Replace("{WTG}", wtgCode);
                else
                    resolvedCanaryTag = resolvedCanaryTag.Replace("WTG01", wtgCode);

                // 產生內部名稱，例如 "WTG01_ActivePower"
                string internalName = $"{wtgCode}_{propertyName}";

                map[resolvedCanaryTag] = internalName;
            }
        }

        return map;
    }

    /// <summary>
    /// 依風機編號對應所屬的 String
    /// </summary>
    private string GetStringByWtgIndex(int index) => index switch
    {
        >= 1 and <= 4   => "WTG_StrA",
        >= 5 and <= 8   => "WTG_StrB",
        >= 9 and <= 12  => "WTG_StrC",
        >= 13 and <= 16 => "WTG_StrD",
        >= 17 and <= 20 => "WTG_StrE",
        >= 21 and <= 25 => "WTG_StrF",
        >= 26 and <= 29 => "WTG_StrG",
        >= 30 and <= 33 => "WTG_StrH", // 假設最後一條跑 5 台
        _ => "WTG_StrA"
    };

}