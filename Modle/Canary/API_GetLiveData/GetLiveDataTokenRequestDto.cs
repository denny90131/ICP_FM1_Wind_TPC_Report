namespace tai_wind_integration.Modle.Canary
{
    public class GetLiveDataTokenRequestDto
    {
        public string ApiToken { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Mode { get; set; } = string.Empty;
        public bool IncludeQuality { get; set; }
    }
}