namespace tai_wind_integration.Modle.SystemConfig
{
    public class BackgroundTaskStatusDto
    {
        public string TaskName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // e.g., "Running", "Success", "Failed", "Skipped"
        public DateTime? LastExecutionTime { get; set; }
        public DateTime? NextScheduledTime { get; set; }
        public string? LastErrorMessage { get; set; }
    }
}