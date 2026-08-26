using tai_wind_integration.Modle.SFTP;

public interface ISftpService
{
    void UploadFile(string localPath, string remotePath);
    void DownloadFile(string remotePath, string localPath);
    void VerifyCommunication();
    // 新增這行
    IEnumerable<SftpItemDto> GetDirectoryStructure(string startPath = "/");
}