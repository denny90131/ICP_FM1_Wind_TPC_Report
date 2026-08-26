using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class SftpController : ControllerBase
{
    private readonly ISftpService _sftpService;

    public SftpController(ISftpService sftpService)
    {
        _sftpService = sftpService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string remotePath)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "未提供上傳檔案。" });

        if (string.IsNullOrWhiteSpace(remotePath))
            remotePath = file.FileName;

        var localPath = Path.GetTempFileName();

        try
        {
            // 1. 寫入暫存檔
            using (var stream = new FileStream(localPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. 呼叫 SFTP 服務 (建議 SFTP 服務也採用非同步 Task)
            _sftpService.UploadFile(localPath, remotePath);

            // 統一屬性名稱為 message
            return Ok(new { message = $"檔案 '{file.FileName}' 已成功上傳至 '{remotePath}'。" });
        }
        catch (Exception ex)
        {
            // 統一屬性名稱為 message
            return StatusCode(500, new { message = ex.Message });
        }
        finally
        {
            if (System.IO.File.Exists(localPath))
            {
                System.IO.File.Delete(localPath);
            }
        }
    }
    [HttpGet("verify")]
    public async Task<IActionResult> Verify()
    {
        try
        {
            // 呼叫 SFTP 服務上傳檔案
            _sftpService.VerifyCommunication();

            return Ok(new { message = $"SFTP通訊驗證成功" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
    [HttpGet("structure")]
    public IActionResult GetStructure([FromQuery] string path = "/")
    {
        try
        {
            // 呼叫 Service 取得樹狀結構資料
            var result = _sftpService.GetDirectoryStructure(path);

            return Ok(new 
            { 
                message = "成功取得 SFTP 結構。", 
                data = result 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}