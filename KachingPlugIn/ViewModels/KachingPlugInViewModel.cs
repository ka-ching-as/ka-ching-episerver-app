namespace KachingPlugIn.ViewModels
{
    public class KachingPlugInViewModel
    {
        public string ProductsImportUrl { get; set; }
        public string TagsImportUrl { get; set; }
        public string FoldersImportUrl { get; set; }
        public bool ProductExportStartButtonDisabled { get; set; }
        public bool CategoryExportStartButtonDisabled { get; set; }
        public ProgressViewModel ProgressViewModel { get; set; }
    }
}