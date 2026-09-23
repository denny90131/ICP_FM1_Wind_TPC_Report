using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

// 根據您的標籤頁，假設資料表名稱為 WindTurbineMetrics
[Table("WindTurbineMetrics")]
public class WindTurbineMetric
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; } // 補上主鍵
    
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