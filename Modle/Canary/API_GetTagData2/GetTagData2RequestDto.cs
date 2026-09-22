public class GetTagData2RequestDto
    {
        public string? ApiToken { get; set; }
        public string? UserToken { get; set; }
        public string? Timezone { get; set; }
        public List<string>? Tags { get; set; }
        public string? Path { get; set; }
        public bool? Deep { get; set; }
        public string? Search { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public string? AggregateName { get; set; }
        public string? AggregateInterval { get; set; }
        public bool? IncludeBounds { get; set; }
        public bool? IncludeQuality { get; set; }
        public bool? UseTimeExtension { get; set; }
        public string? Quality { get; set; }
        public int? MaxSize { get; set; }
        public List<string>? Continuation { get; set; }
    }