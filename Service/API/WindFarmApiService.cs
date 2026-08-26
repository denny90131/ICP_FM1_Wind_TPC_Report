using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

public class WindFarmApiService : IWindFarmApiService
{
    private readonly HttpClient _httpClient;
    private readonly string? _baseUrl;
    private readonly ITokenManager _tokenManager; // 改注入 TokenManager
    private readonly ILogger<WindFarmApiService> _logger;


    public WindFarmApiService(HttpClient httpClient, string? baseUrl, ITokenManager tokenManager, ILogger<WindFarmApiService> logger)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl?.TrimEnd('/') ?? string.Empty;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    /// <summary>
    /// Authentication Header Token驗證
    /// </summary>
    /// <param name="token"></param>
    private void SetAuthorizationHeader(string? token)
    {
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
    /// <summary>
    /// 即時資料api，每5分鐘發送一筆，可預傳一周
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public async Task<WindFarmApiResponse> UploadForecastDataAsync(List<ForecastDataRequest> dataList)
    {
        return await SendWindFarmDataAsync(dataList, "windfarm/forecastData/new", "預測");
    }
    /// <summary>
    /// 即時資料api，每5分鐘發送一次
    /// </summary>
    /// <param name="dataList"></param>
    /// <returns></returns>
    public async Task<WindFarmApiResponse> UploadRealDataAsync(List<RealDataRequest> dataList)
    {
        return await SendWindFarmDataAsync(dataList, "windfarm/realData/new", "即時");
    }

    /// <summary>
    /// 共用的私有泛型方法：負責所有防呆、序列化、HTTP 請求與例外處理
    /// </summary>
    private async Task<WindFarmApiResponse> SendWindFarmDataAsync<T>(List<T> dataList, string apiPath, string dataTypeLabel)
    {
        var currentToken = _tokenManager.GetToken();
        // 1. 防呆檢查：檢查 BaseUrl 或 Token 是否為空
        if (string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(currentToken))
        {
            _logger.LogError("【WindFarmApi Service】Configuration Error: Wind API  Domain or Token is None...");
            return new WindFarmApiResponse { Code = 400, Msg = "Configuration Error: Wind API  Domain or Token is None..." };
        }

        // 2. 防呆檢查：檢查傳入的資料清單是否為空或無資料
        if (dataList == null || dataList.Count == 0)
        {
            _logger.LogError($"【WindFarmApi Service】Failed Upload: The {dataTypeLabel} data list is Empty");
            return new WindFarmApiResponse { Code = 400, Msg = $"Upload failed: The provided {dataTypeLabel} data list cannot be empty." };
        }

        try
        {
            SetAuthorizationHeader(currentToken);
            var url = $"{_baseUrl}/{apiPath}";
            
            
            var jsonPayload = JsonSerializer.Serialize(dataList);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"【WindFarmApi Service】Upload successful: Please verify that data is transmitted properly.");
                return JsonSerializer.Deserialize<WindFarmApiResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? new WindFarmApiResponse { Code = (int)response.StatusCode, Msg = "反序列化失敗" };
            }
            else
            {
                _logger.LogError($"【WindFarmApi Service】Upload failed: API call failed with {response.ReasonPhrase} - {responseString}");
                return new WindFarmApiResponse
                {
                    Code = (int)response.StatusCode,
                    Msg = $"API call failed: {response.ReasonPhrase} - {responseString}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"【WindFarmApi Service】System exception occurred: {ex.Message}");
            return new WindFarmApiResponse
            {
                Code = 500,
                Msg = $"System exception: {ex.Message}"
            };
        }
    }
}