using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

public class WindFarmSyncJob : IWindFarmSyncJob
{
    private readonly ILogger<WindFarmApiService> _logger;
    private readonly IWindFarmApiService _wind;


    public WindFarmSyncJob(ILogger<WindFarmApiService> logger, IWindFarmApiService wind)
    {
        _logger = logger;
        _wind = wind;
    }
    public async Task WindFarmRealTime_RequestAsync()
    {
        var dataList = new List<ForecastDataRequest>
        {
            new ForecastDataRequest
            {
                FarmId = 1,
                DataTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ActiveFansCount = 12,
                AvgPower = 2500.50,
                WindSpeed = 11.4,
            },
            new ForecastDataRequest
            {
                FarmId = 1,
                DataTime = DateTime.Now.AddMinutes(-15).ToString("yyyy-MM-dd HH:mm:ss"),
                ActiveFansCount = 10,
                AvgPower = 2100.00,
                WindSpeed = 9.8,
            }
        };

        await _wind.UploadForecastDataAsync(dataList);
    }
}