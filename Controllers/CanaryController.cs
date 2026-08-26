using Microsoft.AspNetCore.Mvc;
using tai_wind_integration.Modle.Canary;
using System.Linq;

[ApiController]
[Route("api/[controller]")]
public class CanaryController : ControllerBase
{
    private readonly ICanaryService _canaryService;

    public CanaryController(ICanaryService canaryService)
    {
        _canaryService = canaryService;
    }

    [HttpPost("GetLiveDataToken")]
    public async Task<IActionResult> GetLiveDataToken([FromBody] CanaryApiProxyRequest request)
    {
        var externalRequest = new GetLiveDataTokenRequestDto
        {
            Tags = request.Tags.Select(t => t.Value).ToList(),
            Mode = request.Mode,
            IncludeQuality = false
        };

        try
        {
            var apiResponse = await _canaryService.GetLiveDataTokenAsync(externalRequest);

            // 🔴【在這裡印出完整資訊，讓你看到底錯在哪】
            Console.WriteLine($"[DEBUG] IsSuccess: {apiResponse.IsSuccess}");
            Console.WriteLine($"[DEBUG] StatusCode: {apiResponse.StatusCode}");
            Console.WriteLine($"[DEBUG] Data Is Null?: {apiResponse.Data == null}");
            if (apiResponse.Data != null)
            {
                Console.WriteLine($"[DEBUG] Canary Response StatusCode: {apiResponse.Data.StatusCode}");
                Console.WriteLine($"[DEBUG] Canary Response Errors: {apiResponse.Data.Errors}");
            }
            Console.WriteLine($"[DEBUG] RawErrorResponse: {apiResponse.RawErrorResponse}");

            if (apiResponse.IsSuccess)
            {
                if (apiResponse.Data != null && apiResponse.Data.StatusCode == "Good")
                {
                    return Ok(apiResponse.Data);
                }
                
                var error = apiResponse.Data?.Errors ?? apiResponse.Errors ?? "從 Canary API 收到的回應資料為空或狀態不正確。";
                
                // 同時回傳詳細資訊給前端，讓你從瀏覽器或 Postman 也看得見
                return BadRequest(new { 
                    message = "初始化 Token 失敗", 
                    canaryStatusCode = apiResponse.Data?.StatusCode,
                    details = error,
                    raw = apiResponse.RawErrorResponse 
                });
            }

            return StatusCode(apiResponse.StatusCode > 0 ? apiResponse.StatusCode : 500, apiResponse.Errors ?? apiResponse.RawErrorResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXCEPTION] {ex.Message}");
            return StatusCode(500, new { message = "Internal server error.", details = ex.Message });
        }
    }
    
    [HttpPost("RevokeLiveDataToken")]
    public async Task<IActionResult> RevokeLiveDataToken([FromBody] CanaryApiProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LiveDataToken)) // BaseUrl validation removed
        {
            return BadRequest(new { message = "LiveDataToken is required for revocation." });
        }

        var externalRequest = new RevokeLiveDataTokenRequestDto
        {
            LiveDataToken = request.LiveDataToken
        };

        try
        {
            var apiResponse = await _canaryService.RevokeLiveDataTokenAsync(externalRequest);
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                if (apiResponse.Data.StatusCode == "Good")
                {
                    return Ok(apiResponse.Data);
                }
                var error = apiResponse.Data.Errors ?? "Canary API 回應狀態不為 'Good'。";
                return BadRequest(new { message = "註銷 Token 失敗", details = error });
            }

            var failureDetails = apiResponse.Errors ?? apiResponse.RawErrorResponse ?? "從 Canary API 收到的回應資料為空或無法解析。";
            return StatusCode(apiResponse.StatusCode > 0 ? apiResponse.StatusCode : 500, new { message = "呼叫 Canary API 失敗", details = failureDetails });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Internal server error.", details = ex.Message });
        }
    }

    [HttpPost("GetLiveData")]
    public async Task<IActionResult> GetLiveData([FromBody] CanaryApiProxyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LiveDataToken)) // BaseUrl validation removed
        {
            return BadRequest(new { message = "LiveDataToken is required to get live data." });
        }

        var externalRequest = new GetLiveDataRequestDto
        {
            LiveDataToken = request.LiveDataToken,
            Continuation = request.Continuation
        };

        try
        {
            var apiResponse = await _canaryService.GetLiveDataAsync(externalRequest);
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return Ok(apiResponse.Data);
            }

            var failureDetails = apiResponse.Errors ?? apiResponse.RawErrorResponse ?? "從 Canary API 收到的回應資料為空或無法解析。";
            return StatusCode(apiResponse.StatusCode > 0 ? apiResponse.StatusCode : 500, new { message = "抓取資料失敗", details = failureDetails });
        }
        catch (Exception ex) // Catch and log exceptions
        {
            return StatusCode(500, new { message = "Internal server error.", details = ex.Message });
        }
    }

    [HttpPost("TestTagStorageOnly")]
    public async Task<IActionResult> TestTagStorageOnly([FromBody] CanaryApiProxyRequest request)
    {
        // The service method is just a placeholder to align with the interface,
        // but its implementation confirms no external call.
        var result = await _canaryService.TestTagStorageOnly(request);
        return Ok(result);
    }
}