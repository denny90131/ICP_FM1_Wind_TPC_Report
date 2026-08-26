using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class CanaryLogSyncBackground : MainBackground
{
    private readonly IServiceProvider _serviceProvider;

    public CanaryLogSyncBackground(
        IServiceProvider serviceProvider, 
        ILogger<CanaryLogSyncBackground> logger,
        IBackgroundTaskStatusService statusService) // 注入狀態服務
        : base(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(7), logger, statusService) // 將狀態服務傳給基底類別
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        // 建立 DI Scope 取得 Transient/Scoped 的 Job 服務
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<ICanaryLogJob>();
        
        // 取得當前 UTC 時間
        DateTime endTime = DateTime.UtcNow;
        // 抓取過去 1 分鐘的區間
        DateTime strTime = endTime.AddMinutes(-1); 

        // 指定日期寫法
        // DateTime strTime = new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Local);
        // DateTime endTime = new DateTime(2026, 4, 2, 23, 59, 59, 999, DateTimeKind.Local);

        await job.CanaryLogToWindowsEventLog(strTime, endTime);
    }
}