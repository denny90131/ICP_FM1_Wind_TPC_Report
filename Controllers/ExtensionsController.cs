using Microsoft.AspNetCore.Mvc;
using System.Text;
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Text.RegularExpressions;

[Route("api/[controller]")]
public class ExtensionsController : Controller
{    
    private readonly IWindFarmApiService _wind;

    public  ExtensionsController(IWindFarmApiService wind)
    {
        _wind = wind;
    }
    
    /// <summary>
    /// 寄送預測資料
    /// </summary>
    [HttpPost("Forecast")]
    public async Task<IActionResult> UploadForecastData([FromBody] List<ForecastDataRequest> dataList)
    {
        // 基本參數驗證 (改為回傳匿名物件，產生 JSON 格式)
        if (dataList == null || !dataList.Any())
        {
            return BadRequest(new { code = 400, message = "上傳資料不能為空或 JSON 欄位解析失敗。" });
        }

        try
        {
            var result = await _wind.UploadForecastDataAsync(dataList); 
            // 3. 根據風場 API 回傳的 Code 決定 HTTP 狀態碼，方便前端攔截 (例如 401、500)
            if (result.Code == 200 || result.Code == 201)
            {
                return Ok(result);
            }

            return StatusCode(result.Code > 0 ? result.Code : 500, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = 500, message = "內部伺服器錯誤", detail = ex.Message });
        }
    }
    /// <summary>
    /// 寄送當前實際資料
    /// </summary>
    [HttpPost("Real")]
    public async Task<IActionResult> UploadRealData([FromBody] List<RealDataRequest> dataList)
    {
        // 基本參數驗證 (改為回傳匿名物件，產生 JSON 格式)
        if (dataList == null || !dataList.Any())
        {
            return BadRequest(new { code = 400, message = "上傳資料不能為空或 JSON 欄位解析失敗。" });
        }

        try
        {
            var result = await _wind.UploadRealDataAsync(dataList); 
            // 3. 根據風場 API 回傳的 Code 決定 HTTP 狀態碼，方便前端攔截 (例如 401、500)
            if (result.Code == 200 || result.Code == 201)
            {
                return Ok(result);
            }

            return StatusCode(result.Code > 0 ? result.Code : 500, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = 500, message = "內部伺服器錯誤", detail = ex.Message });
        }
    }
}