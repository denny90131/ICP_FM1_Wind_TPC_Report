using System.DirectoryServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using tai_wind_integration.Modle;

public class GroupPermissionSyncService : IGroupPermissionSyncService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GroupPermissionSyncService> _logger;

    // 透過建構子直接注入所需服務
    public GroupPermissionSyncService(
        AppDbContext db,
        IConfiguration configuration,
        ILogger<GroupPermissionSyncService> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 啟動時一併執行：確保 DB 建立 -> 同步權限 -> 同步 AD 群組
    /// </summary>
    public async Task InitializeAsync()
    {
        await _db.Database.EnsureCreatedAsync();
        await SyncPermissionsToDatabaseAsync();
        await SyncAdGroupsToDatabaseAsync();
    }
    
    /// <summary>
    /// 自動同步程式碼定義的權限 (AppPermissions.All) 至 SQLite Permission 資料表
    /// </summary>
    public async Task SyncPermissionsToDatabaseAsync()
    {
        try
        {
            var dbPermissions = await _db.Permissions.ToListAsync();
            var toAdd = new List<Permission>();
            bool hasModified = false;

            foreach (var meta in AppPermissions.All)
            {
                var existing = dbPermissions.FirstOrDefault(p => p.Code.Equals(meta.Code, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    // 程式碼新增的權限 -> 加入待新增清單
                    toAdd.Add(new Permission
                    {
                        Code = meta.Code,
                        Description = meta.Description,
                        Module = meta.Module
                    });
                }
                else
                {
                    // 既有權限若在程式碼中修改了描述或分類 -> 自動更新
                    if (existing.Description != meta.Description || existing.Module != meta.Module)
                    {
                        existing.Description = meta.Description;
                        existing.Module = meta.Module;
                        hasModified = true;
                    }
                }
            }

            if (toAdd.Any())
            {
                await _db.Permissions.AddRangeAsync(toAdd);
                _logger.LogInformation("權限同步成功：新增 {Count} 個系統權限至 SQLite。", toAdd.Count);
            }

            if (toAdd.Any() || hasModified)
            {
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步系統權限至資料庫時發生錯誤。");
        }
    }

    public async Task SyncAdGroupsToDatabaseAsync()
    {
        string ldapPath = _configuration["ActiveDirectory:LdapPath"] ?? "LDAP://fm1.local";

        // 1. 確保資料庫已建立
        await _db.Database.EnsureCreatedAsync();

        // 2. 從 AD 撈取所有群組名稱
        var adGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using (DirectoryEntry rootEntry = new DirectoryEntry(ldapPath))
            using (DirectorySearcher searcher = new DirectorySearcher(rootEntry))
            {
                searcher.Filter = "(objectCategory=group)";
                searcher.PropertiesToLoad.Add("sAMAccountName");

                SearchResultCollection results = searcher.FindAll();
                foreach (SearchResult res in results)
                {
                    if (res.Properties.Contains("sAMAccountName"))
                    {
                        string? groupName = res.Properties["sAMAccountName"][0]?.ToString();
                        if (!string.IsNullOrEmpty(groupName))
                        {
                            adGroupNames.Add(groupName);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "讀取 AD 群組失敗，LDAP 路徑: {LdapPath}", ldapPath);
            return;
        }

        // 3. 取得資料庫中已存在的群組
        var existingGroups = await _db.Roles
            .Select(r => r.AdGroupName)
            .ToListAsync();

        // 4. 比對並將不存在的群組寫入資料庫
        var newGroups = adGroupNames
            .Except(existingGroups, StringComparer.OrdinalIgnoreCase)
            .Select(name => new Role
            {
                Name = name,
                AdGroupName = name
            })
            .ToList();

        if (newGroups.Any())
        {
            await _db.Roles.AddRangeAsync(newGroups);
            await _db.SaveChangesAsync();
            _logger.LogInformation("同步成功：已將 {Count} 個 AD 群組寫入 SQLite 資料庫。", newGroups.Count);
        }
    }
}