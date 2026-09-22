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
    /// 終極聚合主程式：同時抓取平均數據與當前數據，並將它們依照風機 (WTG01~33) 聚合在一起
    /// </summary>
    /// <returns>回傳格式：["WTG01"] = { ["ActivePower"] = 2.42, ["SystemStatus"] = 1 }</returns>
    public async Task<Dictionary<string, Dictionary<string, object?>>> SyncCombinedTurbineDataAsync()
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Canary Reader Sync Job 開始執行 (混合數據聚合模式)...");

        var combinedResult = new Dictionary<string, Dictionary<string, object?>>();

        // 1. 分別取得 AVG(平均) 與 REAL(當前) 的數據包 (裡面包含數值與時間)
        var avgData = await FetchCanaryDataAsync(isAverage: true);
        var realData = await FetchCanaryDataAsync(isAverage: false);

        // 2. 定義一個內部輔助函式，把平坦的字典轉換成分組字典
        void MergeToCombinedResult(Dictionary<string, (object? Value, DateTime? Time)> dataMap)
        {
            foreach (var item in dataMap)
            {
                // item.Key 長相為 "WTG01_ActivePower"
                var parts = item.Key.Split('_', 2);
                if (parts.Length != 2) continue;

                string wtgCode = parts[0];  // "WTG01"
                string propName = parts[1]; // "ActivePower" 或是 "SystemStatus"

                if (!combinedResult.TryGetValue(wtgCode, out var propsDict))
                {
                    propsDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    combinedResult[wtgCode] = propsDict;
                }
                
                // 把屬性與數值塞進去
                combinedResult[wtgCode][propName] = item.Value.Value;
            }
        }

        // 將兩包資料倒進聚合容器裡
        MergeToCombinedResult(avgData);
        MergeToCombinedResult(realData);

        // 3. 輸出漂亮的 Log (包含 WTG01 A:XXX B:XXX 格式)
        _logger.LogInformation("=== 風機混合數據同步明細 (共 {Count} 台) ===", combinedResult.Count);
        foreach (var wtg in combinedResult.OrderBy(x => x.Key))
        {
            // 將該台風機底下的所有屬性組合成字串，例如 "ActivePower: 2.42, SystemStatus: 1"
            string propsString = string.Join(", ", wtg.Value.Select(p => $"{p.Key}: {p.Value ?? "null"}"));
            _logger.LogInformation("[{WTG}] {Props}", wtg.Key, propsString);
        }
        _logger.LogInformation("=========================================");

        sw.Stop();
        _logger.LogInformation("Canary 混合數據聚合完成，總耗時: {Elapsed} ms", sw.ElapsedMilliseconds);

        return combinedResult;
    }

    /// <summary>
    /// 底層共用方法：負責打 API 並解析出 [內部名稱 -> (數值, 時間)]
    /// </summary>
    private async Task<Dictionary<string, (object? Value, DateTime? Time)>> FetchCanaryDataAsync(bool isAverage)
    {
        var result = new Dictionary<string, (object? Value, DateTime? Time)>();
        
        // 根據 isAverage 決定去讀 appsettings.json 裡面的 AVG 還是 REAL 區塊
        var tagMap = GenerateTurbineTagMapping(isAverage);
        if (tagMap.Count == 0) return result;

        var request = new GetTagData2RequestDto
        {
            Tags = tagMap.Keys.ToList(),
            IncludeQuality = true
        };

        // 依照類別設定 API 專屬參數
        if (isAverage)
        {
            request.AggregateName = "TimeAverage2";
            request.AggregateInterval = "00:05:00";
        }
        else
        {
            request.UseTimeExtension = true; // 當前值專用
        }

        var response = await _canaryService.GetTagData2Async(request);

        if (!response.IsSuccess || response.Data?.Data == null)
        {
            _logger.LogError("Canary API [{Mode}] 呼叫失敗: {Errors}", isAverage ? "AVG" : "REAL", response.Errors);
            return result;
        }

        // 轉換成 [內部系統名稱, (數值, 時間)]
        foreach (var (canaryTag, points) in response.Data.Data)
        {
            if (tagMap.TryGetValue(canaryTag, out var internalName))
            {
                var latestPoint = points?.LastOrDefault();
                
                // 時間處理：如果是平均值，將起始時間 + 5 分鐘轉為結算時間
                DateTime? recordTime = null;
                if (latestPoint?.T != null)
                {
                    recordTime = isAverage 
                        ? DateTime.Parse(latestPoint.T.ToString()).AddMinutes(5) 
                        : DateTime.Parse(latestPoint.T.ToString());
                }

                result[internalName] = (latestPoint?.V, recordTime ?? DateTime.Now);
            }
        }

        return result;
    }

    /// <summary>
    /// 解析 appsettings 中的 CanaryTagMapping:AVG 或 CanaryTagMapping:REAL
    /// </summary>
    private Dictionary<string, string> GenerateTurbineTagMapping(bool isAverage)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // 【關鍵】動態決定要抓哪個 JSON 節點
        string targetSection = isAverage ? "CanaryTagMapping:AVG" : "CanaryTagMapping:REAL";
        var mappingSection = _configuration.GetSection(targetSection).GetChildren();

        foreach (var item in mappingSection)
        {
            // 因為 JSON 已經分層，這裡抓到的 Key 就是乾淨的 "ActivePower" 或 "SystemStatus"
            string propertyName = item.Key;   
            string? template = item.Value;    

            if (string.IsNullOrWhiteSpace(template)) continue;

            for (int i = 1; i <= 33; i++)
            {
                string wtgCode = $"WTG{i:D2}";         // WTG01 ~ WTG33
                string strCode = GetStringByWtgIndex(i); // WTG_StrA ~ H

                string resolvedCanaryTag = template;

                if (resolvedCanaryTag.Contains("{STR}"))
                    resolvedCanaryTag = resolvedCanaryTag.Replace("{STR}", strCode);
                else
                    resolvedCanaryTag = resolvedCanaryTag.Replace("WTG_StrA", strCode);

                if (resolvedCanaryTag.Contains("{WTG}"))
                    resolvedCanaryTag = resolvedCanaryTag.Replace("{WTG}", wtgCode);
                else
                    resolvedCanaryTag = resolvedCanaryTag.Replace("WTG01", wtgCode);

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