using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using tai_wind_integration.Modle.SystemConfig;

public abstract class MainBackground : BackgroundService
{
    private readonly TimeSpan _period;
    private readonly TimeSpan _expirationThreshold;
    private readonly ILogger _logger;
    private readonly IBackgroundTaskStatusService _statusService;

    protected MainBackground(
        TimeSpan period, 
        TimeSpan expirationThreshold, 
        ILogger logger,
        IBackgroundTaskStatusService statusService) // 注入狀態服務
    {
        _period = period;
        _expirationThreshold = expirationThreshold;
        _logger = logger;
        _statusService = statusService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var intervalTicks = _period.Ticks;

            var targetTicks = ((now.Ticks + intervalTicks - 1) / intervalTicks) * intervalTicks;
            var targetTime = new DateTime(targetTicks, now.Kind);
            var delay = targetTime - now;

            if (delay <= TimeSpan.Zero)
            {
                targetTime = targetTime.Add(_period);
                delay = targetTime - now;
            }

            try
            {
                // 更新下次執行時間
                _statusService.UpdateStatus(GetType().Name, s => s.NextScheduledTime = targetTime);

                await Task.Delay(delay, stoppingToken);

                if (DateTime.Now > targetTime.Add(_expirationThreshold))
                {
                    _logger.LogWarning("【{ServiceName}】檢測到時間過期，跳過本輪，準備對齊下個刻度", GetType().Name);
                    // 更新狀態為 "已跳過"
                    _statusService.UpdateStatus(GetType().Name, s => {
                        s.Status = "Skipped";
                        s.LastExecutionTime = DateTime.Now;
                        s.LastErrorMessage = "Task skipped due to expiration threshold.";
                    });
                    continue;
                }

                // 更新狀態為 "執行中"
                _statusService.UpdateStatus(GetType().Name, s => s.Status = "Running");

                // 呼叫子類別實作的具體業務邏輯
                await ExecuteTaskAsync(stoppingToken);

                // 成功執行後，更新狀態
                _statusService.UpdateStatus(GetType().Name, s => {
                    s.Status = "Success";
                    s.LastExecutionTime = DateTime.Now;
                    s.LastErrorMessage = null; // 清除上次的錯誤訊息
                });
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "【{ServiceName}】執行任務時發生例外錯誤", GetType().Name);
                // 發生錯誤時，更新狀態
                _statusService.UpdateStatus(GetType().Name, s => {
                    s.Status = "Failed";
                    s.LastExecutionTime = DateTime.Now;
                    s.LastErrorMessage = ex.Message;
                });
            }
        }
    }

    // 抽象方法：讓子類別各自實現邏輯
    protected abstract Task ExecuteTaskAsync(CancellationToken stoppingToken);
}