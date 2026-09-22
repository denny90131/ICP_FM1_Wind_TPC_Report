namespace tai_wind_integration.Modle.PARA
{
    public class CanaryPARA
    {
        /// <summary>
        /// 對應 appsettings 中的 CanaryTagMapping（Key: 屬性名, Value: Tag 路徑或樣板）
        /// </summary>
        public Dictionary<string, string> CanaryTagMapping { get; set; } = new();
    }
}