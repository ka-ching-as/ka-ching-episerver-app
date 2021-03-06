﻿using EPiServer.ServiceLocation;

namespace KachingPlugIn.Services
{
    [ServiceConfiguration(ServiceType = typeof(CategoryExportSingleton), Lifecycle = ServiceInstanceScope.Singleton)]
    public class CategoryExportSingleton: IExportState
    {
        private readonly CategoryExportService _categoryExportService;

        public bool Busy { get; set; }
        public int Uploaded { get; set; }
        public int Total { get; set; }
        public int Polls { get; set; }
        public bool Error { get; set; }
        public string Action { get; set; }
        public string ModelName { get; set; }

        public CategoryExportSingleton(CategoryExportService categoryExportService)
        {
            _categoryExportService = categoryExportService;
            _categoryExportService.ExportState = this;
        }

        public void StartFullCategoryExport()
        {
            _categoryExportService.StartFullCategoryExport();
        }
    }
}