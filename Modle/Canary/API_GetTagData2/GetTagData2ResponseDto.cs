public class GetTagData2ResponseDto
{
    public string? StatusCode { get; set; }
    public List<string>? Errors { get; set; }
    /// <summary>
    /// Key 為 Tag 名稱，Value 為該 Tag 的時間序列點陣列
    /// </summary>
    public Dictionary<string, List<TagDataPoint>>? Data { get; set; }
    public List<string>? Continuation { get; set; }
}