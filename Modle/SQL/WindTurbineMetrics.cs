using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// 根據您的標籤頁，假設資料表名稱為 WindTurbineMetrics
[Table("WindTurbineMetrics")]
// 圖片中未顯示任何主鍵(Key)圖示。如果此表純粹為紀錄用途且無主鍵，必須加上 [Keyless]；若有主鍵，請移除 [Keyless] 並在對應欄位加上 [Key]
[Keyless] 
public class WindTurbineMetric
{
    // 因為圖片中所有欄位都允許 Null，所以 C# 型別全數加上 ? 設為 Nullable
    
    [Column(TypeName = "varchar(15)")]
    public string? FarmId { get; set; }

    [Column(TypeName = "varchar(5)")]
    public string? WTG_Id { get; set; }

    public DateTime? DateTime { get; set; }

    public int? WTG_State { get; set; }

    public int? WTG_HSL { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? Avg_Active_Power { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? Avg_WindSpeed { get; set; }

    [Column(TypeName = "decimal(18, 0)")]
    public decimal? Wind_Direction { get; set; }
}