using Microsoft.Extensions.DependencyInjection.Extensions;
using tai_wind_integration.Modle.SFTP;
using tai_wind_integration.Modle.Canary;
using tai_wind_integration.Modle.SystemConfig;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Negotiate;

public static class ServiceExtensions
{
    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 ILogger
    /// </summary>
    public static void ConfigureSerilog()
    {
        Log.Logger = new LoggerConfiguration()
            // log資訊類型
            .MinimumLevel.Information() // 確保最低層級是 Information
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)

            //  輸出至主控台
            .WriteTo.Console()

            //  輸出至檔案
            .WriteTo.File(
                path: "logs/log_.txt", // 每天自動切換新檔案，檔名如 log_20231027.txt
                rollingInterval: RollingInterval.Day, // 每天建立一新檔
                retainedFileCountLimit: 60, // 超過60天之log自動刪除
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}") // 輸出節點

            //  輸出至Windows Event Log
            .WriteTo.EventLog(
                source: "tai_wind_integration", // 事件來源名稱
                logName: "Application", // Log 類型
                restrictedToMinimumLevel: LogEventLevel.Warning, // 只寫入Warning以上之log
                manageEventSource: false    // 避免因執行階段權限不足報錯
            )

            //創建Logger
            .CreateLogger();
    }
    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 SftpService
    /// </summary>
    public static IServiceCollection AddSftpServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<conf_SFTP>(configuration.GetSection("conf_SFTP"));
        services.AddTransient<ISftpService, SftpService>();
        return services;
    }
    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 WindFarmApiService
    /// </summary>
    public static IServiceCollection AddWindApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 從 appsettings.json 讀取 WindFarmApi 相關設定
        var domain = configuration["conf_API:Domain"] ?? string.Empty;

        // 2. 註冊具有介面對映的 Typed HttpClient (這行會自動把 IWindFarmApiService 與 WindFarmApiService 綁在一起)
        services.AddHttpClient<IWindFarmApiService, WindFarmApiService>((provider, client) =>
        {
            if (!string.IsNullOrEmpty(domain))
            {
                client.BaseAddress = new Uri(domain);
            }
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler())
        // 若要手動傳入 string baseUrl，可以這樣解析：
        .AddTypedClient<IWindFarmApiService>((httpClient, provider) => 
            ActivatorUtilities.CreateInstance<WindFarmApiService>(provider, httpClient, domain));

        return services;
    }
    /// <summary>
    /// 註冊 WindFarm 排程任務服務
    /// </summary>
    public static IServiceCollection AddWindFarmSyncJobs(this IServiceCollection services)
    {
        // 註冊 Job 介面與實作，通常使用 Transient 或 Scoped
        services.AddTransient<IWindFarmSyncJob, WindFarmSyncJob>();

        return services;
    }

    /// <summary>
    /// 註冊 CanaryLog 排程任務服務
    /// </summary>
    public static IServiceCollection AddCanaryLogJob(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<conf_Canary>(config.GetSection("Canary_API")); // Bind to the correct section name
        // 註冊 Job 介面與實作，通常使用 Transient 或 Scoped
        services.AddTransient<ICanaryLogJob, CanaryLogJob>();
        return services;
    }

    /// <summary>
    /// 註冊 CanaryReader 排程任務服務 (讀取風機即時數據 Job)
    /// </summary>
    public static IServiceCollection AddCanaryReaderSyncJob(this IServiceCollection services, IConfiguration config)
    {
        // 1. 將 appsettings 中的 CanaryTagMapping 區段綁定至 CanaryPARA.Tags
        services.Configure<CanaryPARA>(options =>
        {
            options.CanaryTagMapping = config.GetSection("CanaryTagMapping").Get<Dictionary<string, string>>() 
                                        ?? new Dictionary<string, string>();
        });

        // 2. 註冊 Job 介面與實作（通常使用 Scoped 或 Transient）
        services.AddScoped<ICanaryReaderSyncJob, CanaryReaderSyncJob>();

        return services;
    }

    /// <summary>
    /// 註冊背景服務（Background / Hosted Services）
    /// </summary>
    public static IServiceCollection AddWindFarmSyncBackgroundervices(this IServiceCollection services)
    {
        // 註冊背景任務狀態管理服務 (Singleton)
        services.TryAddSingleton<IBackgroundTaskStatusService, BackgroundTaskStatusService>();

        // 註冊 Job 邏輯 (建議使用 Scoped，利於使用 DbContext 或 HttpClient)
        // services.AddScoped<IWindFarmSyncJob, WindFarmSyncJob>();

        services.AddScoped<ICanaryReaderSyncJob, CanaryReaderSyncJob>();
        services.AddScoped<ICsvWritterSyncJob, CsvWritterSyncJob>();

        // 註冊真正的背景排程服務
        services.AddHostedService<WindFarmSyncBackground>();
        return services;
    }
    /// <summary>
    /// 註冊背景服務（Background / Hosted Services）
    /// </summary>
    public static IServiceCollection AddCanarylogSyncBackgroundervices(this IServiceCollection services)
    {
        // 註冊背景任務狀態管理服務 (Singleton)
        services.TryAddSingleton<IBackgroundTaskStatusService, BackgroundTaskStatusService>();

        // 註冊 Job 邏輯 (建議使用 Scoped，利於使用 DbContext 或 HttpClient)
        services.AddScoped<ICanaryLogJob, CanaryLogJob>();

        // 註冊真正的背景排程服務
        services.AddHostedService<CanaryLogSyncBackground>();
        return services;
    }

    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 CanaryService
    /// </summary>
    public static IServiceCollection AddCanaryServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<conf_Canary>(configuration.GetSection("Canary_API")); // Bind to the correct section name
        services.AddHttpClient<ICanaryService, CanaryService>((provider, client) =>
        {
            var canarySettings = provider.GetRequiredService<IOptions<conf_Canary>>().Value;
            if (!string.IsNullOrEmpty(canarySettings.Host) && canarySettings.Port > 0)
            {
                // Construct the base URL from Host and Port
                client.BaseAddress = new Uri($"https://{canarySettings.Host}:{canarySettings.Port}/api/v2"); // Assuming HTTPS and /api/v2 path
            }
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseProxy = false,
            // 略過 SSL 憑證檢查（解決憑證名稱不符或自簽證憑證的錯誤）
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        });
        return services;
    }

    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 FileReader
    /// </summary>
    public static IServiceCollection AddReaderServices(this IServiceCollection services)
    {
        // 1. 維持 Scoped
        services.AddScoped<SubsystemOrchestrator>();

        // 2. 自動註冊所有實作了 ISubsystemModule 的子系統
        var subsystemType = typeof(ISubsystemModule);
        var implementations = subsystemType.Assembly
            .GetTypes()
            .Where(t => subsystemType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

        foreach (var impl in implementations)
        {
            services.AddScoped(subsystemType, impl);
        }

        return services;
    }
    
    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 Sqlite
    /// </summary>
    public static IServiceCollection AddSqliteServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection")));

        return services;
    }

    /// <summary>
    /// 由 ServiceExtensions 統一讀取設定並註冊 Mssql
    /// </summary>
    public static IServiceCollection AddMssqlServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<MssqlDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("MssqlIS&R_Connection")));
            
        // 註冊泛型倉儲 (重要：泛型註冊方式不同於一般型別)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        return services;
    }

    /// <summary>
    /// 註冊 AD 驗證服務
    /// </summary>
    public static IServiceCollection AddActiveDirectoryAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    /// <summary>
    /// 註冊 AD 同步服務
    /// </summary>
    public static IServiceCollection AddActiveDirectorySyncServices(this IServiceCollection services)
    {
        services.AddScoped<IGroupPermissionSyncService, GroupPermissionSyncService>();
        return services;
    }


    /// <summary>
    /// 在應用程式啟動時執行 AD 群組自動同步至 SQLite
    /// </summary>
    public static async Task UseAdGroupAutoSyncAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IGroupPermissionSyncService>();
        await syncService.InitializeAsync();
    }

    /// <summary>
    /// 註冊認證與授權服務（Cookie + Windows AD 整合驗證）
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied"; // 必須指向公開頁面
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
            })
            .AddNegotiate(); // 支援 Windows AD SSO

        // 授權全開設定：只要已登入即通過，不檢查任何特定角色或條件
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // 動態將 AppPermissions.All 中的每一個 Code 註冊為 Policy
            foreach (var meta in AppPermissions.All)
            {
                options.AddPolicy(meta.Code, policy => 
                    policy.RequireClaim("Permission", meta.Code));
            }
        });
        return services;
    }
}

// 事件紀錄檢查
public static class EventLogHelper
{
    // 確認 事件資源 是否存在 windows EventLog，若不存在則創建。
    /// <summary>
    /// 確認事件來源是否存在 Windows EventLog，若不存在則自動檢查權限並建立
    /// </summary>
    public static void EnsureEventSourceExists(string sourceName, string logName = "Application")
    {
        try
        {
            // 1. 透過 Registry 唯讀檢查 (一般權限即可讀取，不會觸發 SecurityException)
            string registryPath = $@"SYSTEM\CurrentControlSet\Services\EventLog\{logName}\{sourceName}";
            using var key = Registry.LocalMachine.OpenSubKey(registryPath);
            if (key != null)
            {
                return; // 已經存在，直接返回
            }

            // 2. 判斷當前程序是否具備管理員權限
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (isAdmin)
            {
                // 當前具備管理員權限，直接建立
                EventLog.CreateEventSource(sourceName, logName);
                Console.WriteLine($"[EventLog] Event Source '{sourceName}' created successfully.");
            }
            else
            {
                // 3. 一般權限時：自動叫起 PowerShell 彈出 Windows UAC 授權建立
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[EventLog] 檢測到 Event Source '{sourceName}' 尚未註冊，正在請求管理員權限建立...");
                Console.ResetColor();

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"New-EventLog -LogName '{logName}' -Source '{sourceName}'\"",
                    Verb = "runas", // 關鍵：彈出 UAC「是/否」視窗
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(startInfo);
                proc?.WaitForExit(); // 等待建立完成

                Console.WriteLine($"[EventLog] Event Source '{sourceName}' 已透過提升權限成功註冊。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EventLog Error] Failed to ensure event source '{sourceName}': {ex.Message}");
        }
    }
}