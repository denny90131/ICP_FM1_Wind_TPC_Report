using tai_wind_integration.Modle.Xlsx;
public interface ISubsystemModule
{
    /// <string>子系統唯一識別碼 (例如: "Wind_Forecast", "Canary_Sync")</string>
    string ModuleId { get; }

    /// <string>子系統顯示名稱</string>
    string ModuleName { get; }

    /// <summary>執行子系統的核心邏輯</summary>
    Task<ExecutionResult> ExecuteAsync(Dictionary<string, object> parameters);
}