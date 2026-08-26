using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

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
    [HttpGet("ForecastDataSummary")] 
    public async Task<IActionResult> ForecastDataSummary()
    {
        return View();
    }

    /// <summary>
    /// CanaryAPI管理頁面
    /// </summary>
    /// <returns></returns>
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
    [HttpGet("LogViewer")] 
    public async Task<IActionResult> EventLog()
    {
        return View();
    }

    /// <summary>
    /// 設置
    /// </summary>
    /// <returns></returns>
    [HttpGet("Setting")] 
    public async Task<IActionResult> Setting()
    {
        return View();
    }
   
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [HttpGet("SFTPManagement")] 
    public async Task<IActionResult> SFTPManagement()
    {
        return View();
    }
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [HttpGet("TaskManagement")] 
    public async Task<IActionResult> TaskManagement()
    {
        return View();
    }
    /// <summary>
    /// SFTP管理頁面
    /// </summary>
    /// <returns></returns>
    [HttpGet("WindTurbineTable")] 
    public async Task<IActionResult> WindTurbineTable()
    {
        return View();
    }
}