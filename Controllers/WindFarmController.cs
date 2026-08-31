using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

[Authorize]
[Route("api/[controller]")]
public class WindFarmController : Controller
{
    private readonly IConfiguration _configuration;

    public WindFarmController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    //預測資料相關
    /// </summary>
    [Authorize(Policy = AppPermissions.ViewBrowser.ForecastDataManager)]
    [HttpGet("ForecastDataSummary")] 
    public async Task<IActionResult> ForecastDataSummary()
    {
        return View();
    }

    /// <summary>
    /// CanaryAPI管理頁面
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.ViewBrowser.CanaryAPIManager)]
    [HttpGet("CanaryAPIManager")] 
    public async Task<IActionResult> CanaryAPIManager()
    {
        var canaryHost = _configuration["Canary_API:Host"];
        var canaryPort = _configuration.GetValue<int>("Canary_API:Port");
        var canaryApiToken = _configuration["Canary_API:API_Token"];

        ViewBag.CanaryApiToken = "********************"; // Mask the token for display in the frontend

        return View();
    }
    

    /// <summary>
    /// 事件日誌
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.ViewBrowser.EventLogViewer)]
    [HttpGet("LogViewer")] 
    public async Task<IActionResult> EventLog()
    {
        return View();
    }

    /// <summary>
    /// 設置
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.SystemAdmin.SystemAdminOption)]
    [HttpGet("Setting")] 
    public async Task<IActionResult> Setting()
    {
        return View();
    }
   
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.ViewBrowser.SFTPManager)]
    [HttpGet("SFTPManagement")] 
    public async Task<IActionResult> SFTPManagement()
    {
        return View();
    }
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.ViewBrowser.TaskManager)]
    [HttpGet("TaskManagement")] 
    public async Task<IActionResult> TaskManagement()
    {
        return View();
    }
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.ViewBrowser.WTGOperator)]
    [HttpGet("WindTurbineTable")] 
    public async Task<IActionResult> WindTurbineTable()
    {
        return View();
    }
    /// <summary>
    /// Role管理頁面
    /// </summary>
    /// <returns></returns>
    [Authorize(Policy = AppPermissions.SystemAdmin.SystemAdminPermission)]
    [HttpGet("RolePermissionManager")] 
    public async Task<IActionResult> RolePermissionManager()
    {
        string ldapPath = _configuration["ActiveDirectory:LdapPath"] ?? "LDAP://fm1.local";
    
        // 去除 LDAP:// 前綴與可能包含的結尾斜線，取得純網域名稱 (fm1.local)
        string domain = ldapPath.Replace("LDAP://", "", StringComparison.OrdinalIgnoreCase).Trim('/');

        ViewBag.LdapDomain = domain;
        return View();
    }
}