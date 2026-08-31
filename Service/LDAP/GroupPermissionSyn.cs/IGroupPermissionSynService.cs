public interface IGroupPermissionSyncService
{
    Task SyncAdGroupsToDatabaseAsync();
    Task SyncPermissionsToDatabaseAsync();
    Task InitializeAsync(); // 整合兩者的一鍵初始化方法
}