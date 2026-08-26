using System.Collections.Generic;

namespace tai_wind_integration.Modle.Canary
{
    public class CanaryApiProxyRequest
    { // BaseUrl is removed as it's now managed by the backend HttpClient's BaseAddress
        public List<CanaryTag> Tags { get; set; } = new List<CanaryTag>();
        public string Mode { get; set; } = string.Empty;
        public string? LiveDataToken { get; set; } // For useExistingToken and revokeToken
        public string? Continuation { get; set; } // For getLiveData
    }
}