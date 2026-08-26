using System.Text.Json.Serialization;

namespace tai_wind_integration.Modle.Canary
{
    public class CanaryApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public int StatusCode { get; set; }
        public object? Errors { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? RawErrorResponse { get; set; }
    }
}