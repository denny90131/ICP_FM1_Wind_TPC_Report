using tai_wind_integration.Modle.Canary;

public interface ICanaryLogJob
{
    Task CanaryLogToWindowsEventLog(DateTime strTime, DateTime endTime);
}