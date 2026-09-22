namespace tai_wind_integration.Modle.Canary
{
    public class RevokeLiveDataTokenResponseDto
    {
        public string StatusCode { get; set; } = string.Empty;
        public string? Message { get; set; }
        public object? Errors { get; set; }
    }
}