using System.Security.Cryptography.X509Certificates;

namespace tai_wind_integration.Modle.Canary
{
    public class conf_Canary
    { // Updated to match appsettings.json structure
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public string API_Token { get; set; } = string.Empty;
        public string MessagePath{get;set;} = string.Empty;
        public string AuditLogPath { get; set; } = string.Empty;
    }
}