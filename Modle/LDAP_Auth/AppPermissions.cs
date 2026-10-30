public static class AppPermissions
{
    public record PermissionMeta(string Code, string Description, string Module);

    public static class ViewBrowser
    {
        public const string ForecastDataManager = "View.ForecastDataManagement";
        public const string CanaryAPIManager = "View.CanaryAPIManager";
        public const string TaskManager = "View.TaskManager";
        public const string WTGOperator = "View.WTGOperator";
        public const string SFTPManager = "View.SFTPManager";
        public const string EventLogViewer = "View.EventLogViewer";
    }

    public static class SystemAdmin
    {
        public const string SystemAdminOption = "SystemAdmin.Option";
        public const string SystemAdminPermission = "SystemAdmin.Permission";
    }

    // 彙整所有清單供啟動時掃描比對
    public static readonly List<PermissionMeta> All = new()
    {
        // 風場模組
        new(ViewBrowser.ForecastDataManager, "View wind farm forecast and real-time data", "View Browser"),
        new(ViewBrowser.CanaryAPIManager, "Configure Canary API data flow", "View Browser"),
        new(ViewBrowser.TaskManager, "Background task schedule manager", "View Browser"),
        new(ViewBrowser.WTGOperator, "WTG status form - Taipower", "View Browser"),
        new(ViewBrowser.SFTPManager, "SFTP communication interface", "View Browser"),
        new(ViewBrowser.EventLogViewer, "Service event log", "View Browser"),

        // 系統管理
        new(SystemAdmin.SystemAdminOption, "Manage global theme and related Token settings", "System Settings"),
        new(SystemAdmin.SystemAdminPermission, "Manage role and permission mapping settings", "System Settings")
    };
}
