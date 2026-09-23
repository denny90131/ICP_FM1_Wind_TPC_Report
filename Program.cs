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
builder.Services.AddMssqlServices(builder.Configuration);  //註冊Mssql服務
builder.Services.AddSqliteServices(builder.Configuration);  //註冊Sqlite服務
builder.Services.AddAuthenticationServices();// 註冊服務
builder.Services.AddActiveDirectoryAuthServices();// 註冊 AD 驗證服務
builder.Services.AddActiveDirectorySyncServices();// 註冊 AD 群組同步服務

builder.Services.AddWindFarmSyncBackgroundervices(); //註冊背景輪詢器 - TPC168
builder.Services.AddCanarylogSyncBackgroundervices(); //註冊背景輪詢器 - Canary Message => Windows Event Log 

builder.Services.AddReaderServices(); //註冊檔案讀取器;

// 註冊Policy
// builder.Services.AddAuthorization(options =>
// {
//     foreach (var meta in AppPermissions.All)
//     {
//         options.AddPolicy(meta.Code, policy => policy.RequireClaim("Permission", meta.Code));
//     }
// });

var app = builder.Build();
app.UseStaticFiles(); // 啟用 wwwroot 中的靜態檔案 (如 css, js)
await app.UseAdGroupAutoSyncAsync(); // 啟動時自動同步 AD 群組到 SQLite
app.UseRouting(); // 解析路由
app.UseCors("AllowAll"); // 套用 CORS 政策處理跨網域
app.UseAuthentication(); // 解析 Cookie / Token 確認身分
app.UseAuthorization();  // 根據身分比對權限 Policy





app.MapGet("/", () => Results.Redirect("/Auth/Login")); // 預設路徑 自動轉向 ForecastDataSummary 總覽頁面

app.MapControllers();

// app.UseHttpsRedirection();

app.Run();
