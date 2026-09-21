using tai_wind_integration.Modle;

public interface ICanaryReaderSyncJob
{
        /// <summary>
        /// 執行 33 台風機數據同步
        /// </summary>
        Task<Dictionary<string, object?>> SyncTurbineDataAsync();
}