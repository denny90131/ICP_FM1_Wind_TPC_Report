using Microsoft.Extensions.Options;
using tai_wind_integration.Modle.Canary;
using System.Net.Http;
using System.Text.Json;
using System.Text;

public class CanaryService : ICanaryService
{
    private readonly ILogger<CanaryService> _logger; // 宣告 Logger
    private readonly conf_Canary _canarySettings; // Inject conf_Canary
    private readonly HttpClient _httpClient;

    // 初次載入時，透過 IOptions 抓取 appsettings 相關設定
    public CanaryService(HttpClient httpClient, ILogger<CanaryService> logger, IOptions<conf_Canary> canarySettings)
    {    
        _logger = logger;
        _canarySettings = canarySettings.Value; // Get the configured settings
        _httpClient = httpClient;
    }

    /// <summary>
    /// 封裝的私有方法，用於發送 POST 請求至 Canary API
    /// </summary>
    private async Task<CanaryApiResponse<TResponse>> SendCanaryApiRequestAsync<TRequest, TResponse>(string relativeUrl, TRequest requestPayload)
    {
        // 如果請求是 GetLiveDataTokenRequestDto，自動填入 API Token
        if (requestPayload is GetLiveDataTokenRequestDto tokenRequest)
        {
            tokenRequest.ApiToken = _canarySettings.API_Token;
        }
        // 如果請求是 GetTagData2RequestDto, 自動填入 API Tolken
        else if (requestPayload is GetTagData2RequestDto tagDataRequest && string.IsNullOrEmpty(tagDataRequest.ApiToken))
        {
            tagDataRequest.ApiToken = _canarySettings.API_Token;
        }
        // 避免因為null 回傳錯誤
        var jsonPayload = JsonSerializer.Serialize(requestPayload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            // 組合完整的 URL 以便於日誌記錄
            var fullUrl = new Uri(_httpClient.BaseAddress, relativeUrl);
            _logger.LogInformation("Calling external Canary API: {Url} with payload: {Payload}", fullUrl, jsonPayload);

            var response = await _httpClient.PostAsync(relativeUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                // 處理 204 No Content 的情況，這通常表示請求成功但沒有回傳資料，對於需要 Token 的請求來說是不正常的
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogWarning("External Canary API at {Url} returned 204 No Content, which is unexpected for this request.", fullUrl);
                    return new CanaryApiResponse<TResponse> { IsSuccess = false, StatusCode = (int)response.StatusCode, Errors = "API 成功執行但未回傳任何內容 (204 No Content)。" };
                }
                var data = JsonSerializer.Deserialize<TResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return new CanaryApiResponse<TResponse> { IsSuccess = true, Data = data, StatusCode = (int)response.StatusCode };
            }
            else
            {
                _logger.LogError("External Canary API call to {Url} failed: {StatusCode} - {Response}", fullUrl, response.StatusCode, responseString);
                // 嘗試解析錯誤回應
                object? errors = null;
                try {
                    var errorData = JsonDocument.Parse(responseString).RootElement;
                    if (errorData.TryGetProperty("errors", out var errorsElement)) {
                        errors = errorsElement.ToString();
                    }
                } catch {
                    errors = responseString;
                }
                return new CanaryApiResponse<TResponse> { IsSuccess = false, StatusCode = (int)response.StatusCode, Errors = errors, RawErrorResponse = responseString };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling external Canary API at {Url}", new Uri(_httpClient.BaseAddress, relativeUrl));
            return new CanaryApiResponse<TResponse> { IsSuccess = false, StatusCode = 500, Errors = ex.Message };
        }
    }

    /// <summary>
    /// 取得Token(自定義標籤)
    /// </summary>
    public async Task<CanaryApiResponse<GetLiveDataTokenResponseDto>> GetLiveDataTokenAsync(GetLiveDataTokenRequestDto request)
    {
        return await SendCanaryApiRequestAsync<GetLiveDataTokenRequestDto, GetLiveDataTokenResponseDto>("v2/getLiveDataToken", request);
    }
    
    /// <summary>
    /// 移除Token(自定義標籤)
    /// </summary>
    public async Task<CanaryApiResponse<RevokeLiveDataTokenResponseDto>> RevokeLiveDataTokenAsync(RevokeLiveDataTokenRequestDto request)
    {
        return await SendCanaryApiRequestAsync<RevokeLiveDataTokenRequestDto, RevokeLiveDataTokenResponseDto>("v2/revokeLiveDataToken", request);
    }
    
    /// <summary>
    /// 依照Token取得資料(自定義標籤)
    /// </summary>
    public async Task<CanaryApiResponse<GetLiveDataResponseDto>> GetLiveDataAsync(GetLiveDataRequestDto request)
    {
        return await SendCanaryApiRequestAsync<GetLiveDataRequestDto, GetLiveDataResponseDto>("v2/getLiveData", request);
    }

    /// <summary>
    /// 取得歷史/原始或統計後的 Tag 數據 (v2/getTagData2)
    /// </summary>
    public async Task<CanaryApiResponse<GetTagData2ResponseDto>> GetTagData2Async(GetTagData2RequestDto request)
    {
        return await SendCanaryApiRequestAsync<GetTagData2RequestDto, GetTagData2ResponseDto>("v2/getTagData2", request);
    }

    // This method is for the "Test Tag Storage Only" functionality, which doesn't call an external API.
    public Task<object> TestTagStorageOnly(CanaryApiProxyRequest request)
    {
        _logger.LogInformation("TestTagStorageOnly called. No external API interaction.");
        return Task.FromResult<object>(new { message = "Tag storage test successful (server-side acknowledged, no external API call)." });
    }
}