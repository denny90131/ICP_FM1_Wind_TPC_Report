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

            // Print full debug details
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
                
                var error = apiResponse.Data?.Errors ?? apiResponse.Errors ?? "Received empty response or invalid status from Canary API.";
                
                // Return detailed info to the client
                return BadRequest(new { 
                    message = "Token initialization failed.", 
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
                var error = apiResponse.Data.Errors ?? "Canary API response status is not 'Good'.";
                return BadRequest(new { message = "Revoke token failed.", details = error });
            }

            var failureDetails = apiResponse.Errors ?? apiResponse.RawErrorResponse ?? "Received empty response or failed to parse response from Canary API.";
            return StatusCode(apiResponse.StatusCode > 0 ? apiResponse.StatusCode : 500, new { message = "Failed to call Canary API.", details = failureDetails });
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

            var failureDetails = apiResponse.Errors ?? apiResponse.RawErrorResponse ?? "Received empty response or failed to parse response from Canary API.";
            return StatusCode(apiResponse.StatusCode > 0 ? apiResponse.StatusCode : 500, new { message = "Failed to fetch live data.", details = failureDetails });
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
