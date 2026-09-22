using tai_wind_integration.Modle;

public interface ICsvWritterSyncJob
{
    /// <summary>
    /// 將風機聚合數據輸出為 CSV 檔案
    /// </summary>
    /// <param name="data">聚合後的風機數據字典 ["WTG01"] = { ["ActivePower"] = 2.42, ... }</param>
    /// <param name="customFileName">自訂檔名（選填，預設以時間戳命名）</param>
    /// <returns>產生的 CSV 完整路徑</returns>
    Task<string?> WriteTurbineDataToCsvAsync(Dictionary<string, Dictionary<string, object?>> data, string? customFileName = null);
}