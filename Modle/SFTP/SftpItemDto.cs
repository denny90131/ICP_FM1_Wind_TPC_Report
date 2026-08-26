namespace tai_wind_integration.Modle.SFTP
{
    public class SftpItemDto
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public List<SftpItemDto> Children { get; set; } = new List<SftpItemDto>();
    }
}