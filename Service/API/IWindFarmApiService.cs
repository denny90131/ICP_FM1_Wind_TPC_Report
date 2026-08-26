public interface IWindFarmApiService
{
    Task<WindFarmApiResponse> UploadForecastDataAsync(List<ForecastDataRequest> dataList);
    Task<WindFarmApiResponse> UploadRealDataAsync(List<RealDataRequest> dataList);
}