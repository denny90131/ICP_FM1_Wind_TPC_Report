using System.Collections.Generic;

namespace tai_wind_integration.Modle.Canary
{
    public class CanaryMessage
    { 
        public int ID { get; set; }
        public long TIMESTAMP{ get; set; }
        public string LOGLEVEL { get; set; }
        public string CATEGORY { get; set; }
        public string MESSAGE { get; set; }
    }
}