using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class WindFarmSyncBackground : MainBackground
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public WindFarmSyncBackground(
        IServiceProvider serviceProvider, 
        ILogger<WindFarmSyncBackground> logger,
        IBackgroundTaskStatusService statusService) // 注入狀態服務
        : base(TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(7), logger, statusService) // 將狀態服務傳給基底類別
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteTaskAsync(CancellationToken stoppingToken)
    {
        // // 建立 DI Scope 取得 Transient/Scoped 的 Job 服務
        // using var scope = _serviceProvider.CreateScope();
        // var job = scope.ServiceProvider.GetRequiredService<IWindFarmSyncJob>();
        // await job.WindFarmRealTime_RequestAsync();

        // 建立 DI Scope 取得 Transient/Scoped 的 Job 服務
        using var scope = _serviceProvider.CreateScope();
        // 取得服務 - Canary 數值採集
        var ReaderJob = scope.ServiceProvider.GetRequiredService<ICanaryReaderSyncJob>();
        // 取得服務 - Canary 數值寫入csv
        var WritterCsvJob = scope.ServiceProvider.GetRequiredService<ICsvWritterSyncJob>();
        // 取得服務 - 取得mssql資料庫 Repository
        var mssql_WindTurbineMetric_Repository = scope.ServiceProvider.GetRequiredService<IRepository<WindTurbineMetric>>();
        
        // 工作流程

        // 採樣 各 WTG 相關數值
        Dictionary<string, TurbineData_Detail> Canary_TurbineData = await ReaderJob.SyncCombinedTurbineDataAsync();

        // --- 統計數值 ---
        // 採樣 全風場5分鐘平均功率
        double? AvgPower_ALL = Canary_TurbineData.CalculateAveragePower();

        // 採樣 線上風機數量
        int? OnlineCount =  Canary_TurbineData.Values.CountByOperationalState(WtgOperationalState.Avail);


        // 將數據轉為 MSSQL 資料格式
        List<WindTurbineMetric> metrics = Canary_TurbineData.ToWindTurbineMetrics();

        // --- 資料注入 ---
        // 寫入 MSSQL-WindTurbineMetric 資料庫
        await mssql_WindTurbineMetric_Repository.AddRangeAsync(metrics);
        await mssql_WindTurbineMetric_Repository.SaveChangesAsync();

        // 寫入Csv進行保存 - csv為設定檔指定路徑
        await WritterCsvJob.WriteTurbineDataToCsvAsync(Canary_TurbineData);
    }
}