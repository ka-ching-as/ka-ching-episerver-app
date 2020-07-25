namespace KachingPlugIn.Web.KachingPlugIn.ViewModels
{
    public class ProgressViewModel
    {
        public bool Busy { get; set; }
        public int Exported { get; set; }
        public int Total { get; set; }
        public int Polls { get; set; }
        public bool Error { get; set; }
        public string Action { get; set; }
        public string ModelName { get; set; }    
    }
}