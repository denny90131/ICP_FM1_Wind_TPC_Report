using System.Collections.Concurrent;
using tai_wind_integration.Modle.SystemConfig;

public class BackgroundTaskStatusService : IBackgroundTaskStatusService
{
    // 使用 ConcurrentDictionary 確保多執行緒安全
    private readonly ConcurrentDictionary<string, BackgroundTaskStatusDto> _statuses = new();

    /// <summary>
    /// 取得指定背景任務的狀態
    /// </summary>
    public BackgroundTaskStatusDto GetStatus(string taskName)
    {
        // 如果找不到，回傳一個預設的初始狀態
        return _statuses.GetOrAdd(taskName, name => new BackgroundTaskStatusDto { TaskName = name });
    }

    /// <summary>
    /// 取得所有背景任務的狀態
    /// </summary>
    public Dictionary<string, BackgroundTaskStatusDto> GetAllStatuses()
    {
        // 回傳一個當前狀態的快照
        return new Dictionary<string, BackgroundTaskStatusDto>(_statuses);
    }

    /// <summary>
    /// 更新指定背景任務的狀態
    /// </summary>
    public void UpdateStatus(string taskName, Action<BackgroundTaskStatusDto> updateAction)
    {
        // GetOrAdd 確保即使鍵不存在也能安全地建立和更新
        var status = _statuses.GetOrAdd(taskName, name => new BackgroundTaskStatusDto { TaskName = name });

        // 鎖定單一狀態物件進行更新，避免競爭條件
        lock (status)
        {
            updateAction(status);
        }
    }
}