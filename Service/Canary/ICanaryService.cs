using tai_wind_integration.Modle.Canary;

public interface ICanaryService
{
    Task<CanaryApiResponse<GetLiveDataTokenResponseDto>> GetLiveDataTokenAsync(GetLiveDataTokenRequestDto request);
    Task<CanaryApiResponse<RevokeLiveDataTokenResponseDto>> RevokeLiveDataTokenAsync(RevokeLiveDataTokenRequestDto request);
    Task<CanaryApiResponse<GetLiveDataResponseDto>> GetLiveDataAsync(GetLiveDataRequestDto request);
    Task<CanaryApiResponse<GetTagData2ResponseDto>> GetTagData2Async(GetTagData2RequestDto request);
    Task<object> TestTagStorageOnly(CanaryApiProxyRequest request);
}