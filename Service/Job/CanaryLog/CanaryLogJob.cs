using Microsoft.Extensions.Options;
using System.Diagnostics;
using tai_wind_integration.Modle.Canary;
using Microsoft.Data.Sqlite;
using Dapper;

public class CanaryLogJob : ICanaryLogJob
{
    private readonly ILogger<CanaryLogJob> _logger; // 宣告 Logger
    private readonly conf_Canary _canarySettings; // Inject conf_Canary
    private const string EventSource = "Canary_Message";
    private const string EventLogType = "Application";

    // 初次載入時，透過 IOptions 抓取 appsettings 相關設定
    public CanaryLogJob(ILogger<CanaryLogJob> logger, IOptions<conf_Canary> canarySettings)
    {    
        _logger = logger;
        _canarySettings = canarySettings.Value; // Get the configured settings
    }

    /// <summary>
    /// 執行主程序邏輯
    /// </summary>
    /// <param name="strTime"></param>
    /// <param name="endTime"></param>
    /// <returns></returns>
    public async Task CanaryLogToWindowsEventLog(DateTime strTime, DateTime endTime)
    {
        // 因 Canary Logsqlite 本身包含 Auditlog，故無需再讀取，參考Canary Log即可。
        // 讀取Sqlite
        List<CanaryMessage> messageTask =  await FetchSqliteLogsAsync(_canarySettings.MessagePath, "Canary Log", strTime, endTime);

        // Task<List<CanaryMessage>> auditTask = FetchSqliteLogsAsync(_canarySettings.AuditLogPath, "Canary AuditLog", strTime, endTime);

        // 平行等待兩個 Task 完成
        // await Task.WhenAll(messageTask, auditTask);
        // List<CanaryMessage> messages = await messageTask;
        // List<CanaryMessage> auditLogs = await auditTask;

        // 3. 合併並依時間排序
        // List<CanaryMessage> combinedLogs = messages
        //     .Concat(auditLogs)
        //     .OrderBy(x => x.TIMESTAMP)
        //     .ToList();
        
        //寫入Windows Event Log
        WriteToWindowsEventLog(messageTask);
    }
    
    /// <summary>
    /// 通用 SQLite 查詢方法
    /// </summary>
    private async Task<List<CanaryMessage>> FetchSqliteLogsAsync(string? dbPath, string logTypeName, DateTime strTime, DateTime endTime)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            _logger.LogWarning("{LogTypeName} path is empty or not configured.", logTypeName);
            return new List<CanaryMessage>();
        }

        try
        {
            long startTicks = strTime.ToUniversalTime().Ticks;
            long endTicks = endTime.ToUniversalTime().Ticks;

            string connectionString = $"Data Source={dbPath};Mode=ReadOnly;";
            using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM EVENTS WHERE TimeStamp >= @StartTicks AND TimeStamp <= @EndTicks";

            IEnumerable<CanaryMessage> result = await connection.QueryAsync<CanaryMessage>(sql, new 
            { 
                StartTicks = startTicks, 
                EndTicks = endTicks 
            });

            return result.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read SQLite {LogTypeName}. Path: {Path}, Start: {Start}, End: {End}",
                logTypeName, dbPath, strTime, endTime);

            return new List<CanaryMessage>();
        }
    }

    /// <summary>
    /// 寫入 Windows Event Log
    /// </summary>
    private void WriteToWindowsEventLog(List<CanaryMessage> messages)
    {
        if (messages == null || messages.Count == 0) return;
        try
        {
            //檢查依然需要管理員權限，若非管理員權限，則嘗試直接寫入，避免跳出
            try
            {
                // 檢查並建立事件來源 (需要系統管理員權限才能建立新 Source)
                if (!EventLog.SourceExists(EventSource))EventLog.CreateEventSource(EventSource, EventLogType);
            }
            catch
            {
                
            }


            //
            using var eventLog = new EventLog(EventLogType);
            eventLog.Source = EventSource;

            foreach (var msg in messages)
            {

                // 組裝要顯示在 Windows 事件檢視器中的內容
                string logContent = $"[Canary Event]\nTime: {new DateTime(msg.TIMESTAMP, DateTimeKind.Utc).ToLocalTime():yyyy-MM-dd HH:mm:ss}\nMessage: {msg.MESSAGE}";
                
                // 寫入 Event Log (可自訂 Event ID，例如 1001)
                eventLog.WriteEntry(logContent, LogLevelConvert(msg.LOGLEVEL), 1001);
            }
        }
        catch(System.Security.SecurityException ex)
        {
            _logger.LogError(ex, "Insufficient permissions to check or create Windows Event Source: {Source}. Please register the event source manually with administrator privileges.", EventSource);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "An error occurred while writing to the Windows Event Log.");
        }        
    }
    
    /// <summary>
    /// 依據 canary log 判斷 windows event log 之類型
    /// </summary>
    private EventLogEntryType LogLevelConvert(string? loglevel) =>
        loglevel?.Trim().ToLowerInvariant() switch
        {
            "warn" or "warning" => EventLogEntryType.Warning,
            "error"             => EventLogEntryType.Error,
            _                   => EventLogEntryType.Information
        };
}