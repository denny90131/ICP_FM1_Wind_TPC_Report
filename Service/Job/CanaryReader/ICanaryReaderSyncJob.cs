using tai_wind_integration.Modle;

public interface ICanaryReaderSyncJob
{
    Task<Dictionary<string, Dictionary<string, object?>>> SyncCombinedTurbineDataAsync();
}