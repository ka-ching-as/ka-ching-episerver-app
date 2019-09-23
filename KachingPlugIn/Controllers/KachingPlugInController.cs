using EPiServer.Logging;
using EPiServer.PlugIn;
using KachingPlugIn.Helpers;
using KachingPlugIn.Models;
using KachingPlugIn.Services;
using KachingPlugIn.ViewModels;
using System.Web.Mvc;

namespace KachingPlugIn.Controllers
{
    [GuiPlugIn(
        Area = PlugInArea.AdminMenu,
        Url = "/modules/KachingPlugIn/Controllers",
        LanguagePath = "/modules/KachingPlugIn/Language",
        DisplayName = "Ka-ching Integration")]
    public class KachingPlugInController : Controller
    {
        private ProductExportSingleton _productExport;
        private CategoryExportSingleton _categoryExport;
        private readonly ILogger _log = LogManager.GetLogger(typeof(KachingPlugInController));

        public KachingPlugInController(
            ProductExportSingleton productExport,
            CategoryExportSingleton categoryExport)
        {
            _productExport = productExport;
            _categoryExport = categoryExport;
        }

        public ActionResult Index()
        {
            var configuration = Configuration.Instance();

            var viewModel = new KachingPlugInViewModel();
            viewModel.ProgressViewModel = BuildProgressViewModel();
            viewModel.ProductsImportUrl = configuration.ProductsImportUrl;
            viewModel.TagsImportUrl = configuration.TagsImportUrl;
            viewModel.FoldersImportUrl = configuration.FoldersImportUrl;
            viewModel.ProductExportStartButtonDisabled = !configuration.ProductsImportUrl.IsValidProductsImportUrl();
            viewModel.CategoryExportStartButtonDisabled = !configuration.TagsImportUrl.IsValidTagsImportUrl() || !configuration.FoldersImportUrl.IsValidFoldersImportUrl();

            return PartialView(viewModel);
        }

        [HttpPost]
        public ActionResult StartFullProductExport()
        {
            var configuration = Configuration.Instance();
            _productExport.StartFullProductExport(configuration.ProductsImportUrl);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult StartFullCategoryExport()
        {
            _log.Information("StartFullCategoryExport");
            var configuration = Configuration.Instance();
            _categoryExport.StartFullCategoryExport(configuration.TagsImportUrl, configuration.FoldersImportUrl);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateProductImportUrl(string ProductsImportUrl)
        {
            _log.Information("UpdateProductImportUrl: " + ProductsImportUrl);
            var configuration = Configuration.Instance();
            configuration.ProductsImportUrl = ProductsImportUrl;
            configuration.Save();
            return RedirectToAction("Index");

        }

        [HttpPost]
        public ActionResult UpdateTagsImportUrl(string TagsImportUrl)
        {
            _log.Information("UpdateTagsImportUrl: " + TagsImportUrl);
            var configuration = Configuration.Instance();
            configuration.TagsImportUrl = TagsImportUrl;
            configuration.Save();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult UpdateFoldersImportUrl(string FoldersImportUrl)
        {
            _log.Information("UpdateFoldersImportUrl: " + FoldersImportUrl);
            var configuration = Configuration.Instance();
            configuration.FoldersImportUrl = FoldersImportUrl;
            configuration.Save();
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Poll()
        {
            if (_productExport.Busy)
            {
                _productExport.Polls += 1;
            }
            else if (_categoryExport.Busy)
            {
                _categoryExport.Polls += 1;
            }
            
            return PartialView("Progress", BuildProgressViewModel());
        }

        private ProgressViewModel BuildProgressViewModel()
        {
            var result = new ProgressViewModel();
            if(_productExport.Busy)
            {
                result.Busy = _productExport.Busy;
                result.Total = _productExport.Total;
                result.Exported = _productExport.Uploaded;
                result.Polls = _productExport.Polls;
                result.Error = _productExport.Error;
                result.ModelName = "products";
            }
            else if (_categoryExport.Busy)
            {
                result.Busy = _categoryExport.Busy;
                result.Total = _categoryExport.Total;
                result.Exported = _categoryExport.Uploaded;
                result.Polls = _categoryExport.Polls;
                result.Error = _categoryExport.Error;
                result.ModelName = "tags and folders";
            }
            else
            {
                result.Busy = false;
                result.Total = 0;
                result.Exported = 0;
                result.Polls = 0;
                result.Error = false;
                result.ModelName = string.Empty;
            }
            
            return result;
        }
    }
}