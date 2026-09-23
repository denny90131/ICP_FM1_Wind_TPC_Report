using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class CsvWritterSyncJob : ICsvWritterSyncJob
{
    private readonly ILogger<CsvWritterSyncJob> _logger;
    private readonly IConfiguration _configuration;

    public CsvWritterSyncJob(
        ILogger<CsvWritterSyncJob> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// 將風機數據寫入當天的 CSV 檔案 (一天一張表，採累加追加模式)
    /// </summary>
    public async Task<string?> WriteTurbineDataToCsvAsync(
        Dictionary<string, TurbineData_Detail> data, 
        string? customFileName = null)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("開始執行 CSV 追加寫入作業 (強型別模式)...");

        if (data == null || data.Count == 0)
        {
            _logger.LogWarning("傳入的風機數據為空，中止寫入。");
            return null;
        }

        try
        {
            // 1. 取得目標資料夾路徑
            string? targetFolder = GetWtgTableFolderPath();
            if (string.IsNullOrWhiteSpace(targetFolder))
            {
                _logger.LogError("找不到 conf_review:Modules 中第一個模組的 Folder_Path 設定。");
                return null;
            }

            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }

            // 2. 決定按天命名的檔名（例如：WTG_Table_20260922.csv）
            string fileName = string.IsNullOrWhiteSpace(customFileName)
                ? $"WTG_Table_{DateTime.Now:yyyyMMdd}.csv"
                : customFileName;

            if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                fileName += ".csv";
            }

            string fullFilePath = Path.Combine(targetFolder, fileName);
            bool isFileExists = File.Exists(fullFilePath);

            // 3. 收集 DynamicAttributes 兜底屬性欄位 (若有未定義點位)
            var dynamicKeys = data.Values
                .SelectMany(d => d.DynamicAttributes.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(k => k)
                .ToList();

            var csvBuilder = new StringBuilder();

            // 4. 若當天檔案「不存在」，寫入 Header
            if (!isFileExists)
            {
                var headers = new List<string>
                {
                    "DateTime",
                    "TurbineId",
                    "ActivePower",
                    "WindSpeed",
                    "AbsoluteWindDirection",
                    "WTG_HSL",
                    "OperatorState",
                    "ServiceState",
                    "WindTurbine" // 運轉狀態字判斷結果 (Enum)
                };

                // 若有擴充點位，追加在後端
                headers.AddRange(dynamicKeys);

                csvBuilder.AppendLine(string.Join(",", headers.Select(EscapeCsvField)));
            }

            // 5. 逐列組裝風機數據 (WTG01 ~ WTG33)
            foreach (var item in data.OrderBy(x => x.Key))
            {
                var turbine = item.Value;
                string wtgId = !string.IsNullOrWhiteSpace(turbine.TurbineId) ? turbine.TurbineId : item.Key;

                // 時間戳記：若該風機沒有取到時間點，則以當下時間遞補
                string timeStr = turbine.Timestamp.HasValue
                    ? turbine.Timestamp.Value.ToString("yyyy-MM-dd HH:mm:ss")
                    : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var rowValues = new List<string>
                {
                    EscapeCsvField(timeStr),
                    EscapeCsvField(wtgId),
                    EscapeCsvField(turbine.ActivePower?.ToString("F2")),
                    EscapeCsvField(turbine.WindSpeed?.ToString("F2")),
                    EscapeCsvField(turbine.AbsoluteWindDirection?.ToString("F2")),
                    EscapeCsvField(turbine.WTG_HSL?.ToString("F2")),
                    EscapeCsvField(turbine.OperatorState?.ToString()),
                    EscapeCsvField(turbine.ServiceState?.ToString()),
                    EscapeCsvField(turbine.WindTurbine?.ToString())
                };

                // 填入 DynamicAttributes 的數值
                foreach (var dynKey in dynamicKeys)
                {
                    string dynVal = turbine.DynamicAttributes.TryGetValue(dynKey, out var val) && val != null
                        ? val.ToString() ?? ""
                        : "";
                    rowValues.Add(EscapeCsvField(dynVal));
                }

                csvBuilder.AppendLine(string.Join(",", rowValues));
            }

            // 6. 寫入檔案（帶 BOM 的 UTF-8，不存在則新建，存在則 Append 追加）
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: !isFileExists);
            await File.AppendAllTextAsync(fullFilePath, csvBuilder.ToString(), encoding);

            sw.Stop();
            _logger.LogInformation("CSV 寫入成功！檔案路徑: {Path}，共寫入 {Count} 台風機記錄，耗時: {Elapsed} ms", 
                fullFilePath, data.Count, sw.ElapsedMilliseconds);

            return fullFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "寫入 CSV 檔案時發生未預期錯誤。");
            return null;
        }
    }

    /// <summary>
    /// 解析 conf_review:Modules 取得第一個模組的 Folder_Path
    /// </summary>
    private string? GetWtgTableFolderPath()
    {
        var firstModule = _configuration.GetSection("conf_review:Modules")
                                        .GetChildren()
                                        .FirstOrDefault();

        return firstModule?["Folder_Path"];
    }

    /// <summary>
    /// 處理 CSV 特殊字元 (逗號、引號、換行) 的轉義
    /// </summary>
    private static string EscapeCsvField(string? field)
    {
        if (string.IsNullOrEmpty(field)) return "";

        if (field.Contains(',') || field.Contains('"') || field.Contains('\r') || field.Contains('\n'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }
}