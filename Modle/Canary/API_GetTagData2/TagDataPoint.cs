public class TagDataPoint
    {
        public string? T { get; set; }       // Timestamp
        public object? V { get; set; }       // Value (數值、字串或布林)
        public int? Q { get; set; }          // Quality
    }