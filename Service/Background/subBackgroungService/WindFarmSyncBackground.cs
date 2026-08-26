using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class WindFarmSyncBackground : MainBackground
{
    private readonly IServiceProvider _serviceProvider;

    public WindFarmSyncBackground(
        IServiceProvider serviceProvider, 
        ILogger<WindFarmSyncBackground> logger,
        IBackgroundTaskStatusService statusService) // 注入狀態服務
        : base(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(7), logger, statusService) // 將狀態服務傳給基底類別
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        // 建立 DI Scope 取得 Transient/Scoped 的 Job 服務
        using var scope = _serviceProvider.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<IWindFarmSyncJob>();
        
        await job.WindFarmRealTime_RequestAsync();
    }
}