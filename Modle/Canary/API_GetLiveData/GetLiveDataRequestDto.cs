namespace tai_wind_integration.Modle.Canary
{
    public class GetLiveDataRequestDto
    {
        public string LiveDataToken { get; set; } = string.Empty;
        public string? Continuation { get; set; }
    }
}