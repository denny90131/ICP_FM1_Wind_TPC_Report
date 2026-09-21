using System.Text.Json.Serialization;

namespace tai_wind_integration.Modle.Canary
{
    public class GetLiveDataResponseDto
    {
        [JsonPropertyName("continuation")]
        public string? Continuation { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? Data { get; set; } // To capture all other dynamic properties
    }
}