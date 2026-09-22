public sealed class TurbineData_Detail
{
    // 風機編號
    public string TurbineId { get; set; } = string.Empty;
    // 時間戳記
    public DateTime? Timestamp { get; set; }
    // [平均值 / 5 min] 有效功率 - 來自電網Active Power
    public double? ActivePower { get; set; }
    // [即時值] 風速 - 來自風機提供之風速
    public double? WindSpeed { get; set; }
    // [即時值] 絕對方位 - 來自風機提供之方位
    public double? AbsoluteWindDirection { get; set; }
    // (來源尚未定義) 風機HSL
    public double? WTG_HSL {get; set;} = 0;
    // [即時值] 運轉字 - 來自風機運轉狀態
    public int? OperatorState { get; set; }
    // [即時值] 服務字 - 來自風機服務狀態
    public int? ServiceState { get; set; }

    // 自主邏輯判斷 (運轉字 + 服務字)
    public WtgOperationalState? WindTurbine
    {
        get
        {
            //如為運
            if (!OperatorState.HasValue || !ServiceState.HasValue)return null;
            return ServiceState.Value switch
            {
                1 => WtgOperationalState.Out,
                2 => WtgOperationalState.Test,
                0 => OperatorState.Value switch
                {
                    0 or 1 => WtgOperationalState.Out,
                    2 => WtgOperationalState.Lshed,
                    3 => WtgOperationalState.Avail,
                    _ => null
                },
                _ => null
            };
        }
    }

    // 兜底字典：存放未定義的點位
    public Dictionary<string, object?> DynamicAttributes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 依照屬性名稱自動映射到強型別屬性
    /// </summary>
    public void SetProperty(string propName, object? rawValue, DateTime? pointTime = null)
    {
        // 💡 2. 隨點位時間更新 Timestamp (取最新時間)
        if (pointTime.HasValue)
        {
            if (!Timestamp.HasValue || pointTime.Value > Timestamp.Value)
            {
                Timestamp = pointTime.Value;
            }
        }

        switch (propName.ToUpperInvariant())
        {
            case "ACTIVEPOWER":
                ActivePower = SafeToDouble(rawValue);
                break;
            case "WINDSPEED":
                WindSpeed = SafeToDouble(rawValue);
                break;
            case "ABSOLUTEWINDDIRECTION":
                AbsoluteWindDirection = SafeToDouble(rawValue);
                break;
            case "OPERATORSTATUS":
                OperatorState = SafeToInt(rawValue);
                break;
            case "SERVICESTATUS":
                ServiceState = SafeToInt(rawValue);
                break;
            default:
                DynamicAttributes[propName] = rawValue;
                break;
        }
    }

    // --- 安全轉型 Helper，避免字串 "null"、DBNull 或無效字串導致程式 Crash ---
    // --- Double ---
    private static double? SafeToDouble(object? val)
    {
        if (val is null or DBNull) return null;
        if (val is double d) return d;
        if (val is float f) return (double)f;
        if (val is string s && (string.IsNullOrWhiteSpace(s) || s.Equals("null", StringComparison.OrdinalIgnoreCase)))
            return null;

        return double.TryParse(val.ToString(), out var result) ? result : null;
    }
    
    // --- Int ---
    private static int? SafeToInt(object? val)
    {
        if (val is null or DBNull) return null;
        if (val is int i) return i;
        if (val is string s && (string.IsNullOrWhiteSpace(s) || s.Equals("null", StringComparison.OrdinalIgnoreCase)))
            return null;

        return int.TryParse(val.ToString(), out var result) ? result : null;
    }
}