namespace KachingPlugIn.Services
{
    public interface IExportState
    {
        bool Busy { get; set; }
        int Uploaded { get; set; }
        int Total { get; set; }
        int Polls { get; set; }
        bool Error { get; set; }
    }
}
