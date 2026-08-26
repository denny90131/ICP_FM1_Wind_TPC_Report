namespace tai_wind_integration.Modle.Xlsx
{
    public class ConfReviewOptions
    {
        public WtgTableConfig WTG_Table { get; set; } = new();
        // 如果未來還有其他子系統，可以繼續加在這裡
    }
    public class WtgTableConfig
    {
      public string Folder_Path {get; set;} = string.Empty;
      public string File_Type {get; set;} = string.Empty;
    }
}