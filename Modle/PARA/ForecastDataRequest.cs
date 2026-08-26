public class ForecastDataRequest
{
    public int FarmId { get; set; }
    public string DataTime { get; set; }
    public int Status { get; set; }
    public int ActiveFansCount { get; set; }
    public double MaxPower { get; set; }
    public double WindSpeed { get; set; }
    public double AvgPower { get; set; }
}