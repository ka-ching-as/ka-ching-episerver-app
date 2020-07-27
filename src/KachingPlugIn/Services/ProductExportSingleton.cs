using EPiServer.ServiceLocation;

namespace KachingPlugIn.Services
{
    [ServiceConfiguration(ServiceType = typeof(ProductExportSingleton), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ProductExportSingleton: IExportState
    {
        private readonly ProductExportService _productExportService;

        public bool Busy { get; set; }
        public int Uploaded { get; set; }
        public int Total { get; set; }
        public int Polls { get; set; }
        public bool Error { get; set; }
        public string Action { get; set; }
        public string ModelName { get; set; }

        public ProductExportSingleton(ProductExportService productExportService)
        {
            _productExportService = productExportService;
            _productExportService.ExportState = this;
        }

        public void StartFullProductExport(string url)
        {
            _productExportService.StartFullProductExport(url);
        }
    }
}