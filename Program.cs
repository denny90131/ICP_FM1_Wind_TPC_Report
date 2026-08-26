using Serilog;

EventLogHelper.EnsureEventSourceExists("tai_wind_integration");
Serilog.Debugging.SelfLog.Enable(Console.Error);
ServiceExtensions.ConfigureSerilog();

var builder = WebApplication.CreateBuilder(args);

// 1. 註冊 CORS 服務
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Host.UseSerilog(); //Serilog註冊
builder.Services.AddControllersWithViews(); // MVC註冊
builder.Services.AddSingleton<ITokenManager, TokenManager>();
builder.Services.AddSftpServices(builder.Configuration); // SFTP 註冊
builder.Services.AddCanaryServices(builder.Configuration); // Canary API 服務註冊
builder.Services.AddWindApiServices(builder.Configuration); //註冊風場 API 服務

builder.Services.AddWindFarmSyncBackgroundervices(); //註冊背景輪詢器 - TPC168
builder.Services.AddCanarylogSyncBackgroundervices(); //註冊背景輪詢器 - Canary Message => Windows Event Log 

builder.Services.AddReaderServices(); //註冊檔案讀取器;


var app = builder.Build();

// 2. 套用 CORS 政策 (必須在 MapControllers 之前)
app.UseCors("AllowAll");

app.UseStaticFiles(); // <-- 加入這行來啟用 wwwroot 中的靜態檔案 (如 css, js)

// 預設路徑 自動轉向 ForecastDataSummary 總覽頁面
app.MapGet("/", () => Results.Redirect("/api/WindFarm/ForecastDataSummary"));

app.MapControllers();

// app.UseHttpsRedirection();

app.Run();
