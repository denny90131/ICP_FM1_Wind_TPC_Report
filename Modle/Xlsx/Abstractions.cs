namespace tai_wind_integration.Modle.Xlsx
{
    // 子系統執行的統一結果回傳格式
    public class ExecutionResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
