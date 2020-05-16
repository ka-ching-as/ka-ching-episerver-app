namespace KachingPlugIn.ViewModels
{
    public class PlugInViewModel
    {
        public bool ExportSingleVariantAsProduct { get; set; }
        public string ProductsImportUrl { get; set; }
        public string TagsImportUrl { get; set; }
        public string FoldersImportUrl { get; set; }
        public bool ProductExportStartButtonDisabled { get; set; }
        public bool CategoryExportStartButtonDisabled { get; set; }
        public ProgressViewModel ProgressViewModel { get; set; }
    }
}