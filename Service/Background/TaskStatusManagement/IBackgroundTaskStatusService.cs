using tai_wind_integration.Modle.SystemConfig;

public interface IBackgroundTaskStatusService
{
    void UpdateStatus(string taskName, Action<BackgroundTaskStatusDto> updateAction);
    BackgroundTaskStatusDto GetStatus(string taskName);
    Dictionary<string, BackgroundTaskStatusDto> GetAllStatuses();
}