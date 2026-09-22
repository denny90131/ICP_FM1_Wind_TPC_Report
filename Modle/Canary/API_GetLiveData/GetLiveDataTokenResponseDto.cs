namespace tai_wind_integration.Modle.Canary
{
    public class GetLiveDataTokenResponseDto
    {
        public string StatusCode { get; set; } = string.Empty;
        public string? LiveDataToken { get; set; }
        public object? Errors { get; set; } // Can be string or object
    }
}