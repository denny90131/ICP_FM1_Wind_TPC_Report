using tai_wind_integration.Modle;

public interface ICanaryReaderSyncJob
{
    Task<Dictionary<string, TurbineData_Detail>> SyncCombinedTurbineDataAsync();
}