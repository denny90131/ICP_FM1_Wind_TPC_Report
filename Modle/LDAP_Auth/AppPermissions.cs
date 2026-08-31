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
        new(ViewBrowser.ForecastDataManager, "查看風場預測與即時數據", "畫面瀏覽"),
        new(ViewBrowser.CanaryAPIManager, "配置 Canary API 資料流", "畫面瀏覽"),
        new(ViewBrowser.TaskManager, "背景工作排程管理員", "畫面瀏覽"),
        new(ViewBrowser.WTGOperator, "渢妙風機狀態表單-台電", "畫面瀏覽"),
        new(ViewBrowser.SFTPManager, "SFTP通訊介接", "畫面瀏覽"),
        new(ViewBrowser.EventLogViewer, "服務事件紀錄", "畫面瀏覽"),

        // 系統管理
        new(SystemAdmin.SystemAdminOption, "管理畫面全域主題及相關Token設置", "系統設定"),
        new(SystemAdmin.SystemAdminPermission, "管理角色與權限映射設定", "系統設定")
    };
}