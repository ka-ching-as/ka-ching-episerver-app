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
        Url = "/Episerver/KachingPlugIn/KachingPlugIn",
        LanguagePath = "/modules/KachingPlugIn/EmbeddedLangFiles",
        DisplayName = "Ka-ching Integration")]
    public class KachingPlugInController : Controller
    {
        private readonly ProductExportSingleton _productExport;
        private readonly CategoryExportSingleton _categoryExport;
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

            var viewModel = new PlugInViewModel();
            viewModel.ProgressViewModel = BuildProgressViewModel();
            viewModel.ProgressViewLocation = ViewLocation("Progress");
            viewModel.ProductsImportUrl = configuration.ProductsImportUrl;
            viewModel.TagsImportUrl = configuration.TagsImportUrl;
            viewModel.FoldersImportUrl = configuration.FoldersImportUrl;
            viewModel.ProductExportStartButtonDisabled = !configuration.ProductsImportUrl.IsValidProductsImportUrl();
            viewModel.CategoryExportStartButtonDisabled = !configuration.TagsImportUrl.IsValidTagsImportUrl() || !configuration.FoldersImportUrl.IsValidFoldersImportUrl();

            return View("Index", viewModel);
        }

        [HttpPost]
        public ActionResult StartFullProductExport()
        {
            var configuration = Configuration.Instance();
            _productExport.StartFullProductExport(configuration.ProductsImportUrl);
            return RedirectToAction("Index", "KachingPlugIn");
        }

        [HttpPost]
        public ActionResult StartFullCategoryExport()
        {
            _log.Information("StartFullCategoryExport");
            var configuration = Configuration.Instance();
            _categoryExport.StartFullCategoryExport(configuration.TagsImportUrl, configuration.FoldersImportUrl);
            return RedirectToAction("Index", "KachingPlugIn");
        }

        [HttpPost]
        public ActionResult UpdateProductImportUrl(string productsImportUrl)
        {
            _log.Information("UpdateProductImportUrl: " + productsImportUrl);
            var configuration = Configuration.Instance();
            configuration.ProductsImportUrl = productsImportUrl;
            configuration.Save();
            return RedirectToAction("Index", "KachingPlugIn");
        }

        [HttpPost]
        public ActionResult UpdateTagsImportUrl(string tagsImportUrl)
        {
            _log.Information("UpdateTagsImportUrl: " + tagsImportUrl);
            var configuration = Configuration.Instance();
            configuration.TagsImportUrl = tagsImportUrl;
            configuration.Save();
            return RedirectToAction("Index", "KachingPlugIn");
        }

        [HttpPost]
        public ActionResult UpdateFoldersImportUrl(string foldersImportUrl)
        {
            _log.Information("UpdateFoldersImportUrl: " + foldersImportUrl);
            var configuration = Configuration.Instance();
            configuration.FoldersImportUrl = foldersImportUrl;
            configuration.Save();
            return RedirectToAction("Index", "KachingPlugIn");
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

            return PartialView(ViewLocation("Progress"), BuildProgressViewModel());
        }

        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (!PrincipalInfo.HasAdminAccess)
            {
                throw new UnauthorizedAccessException();
            }

            base.OnAuthorization(filterContext);
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

        private string ViewLocation(string viewName)
        {
            return $"{EPiServer.Shell.Paths.ProtectedRootPath}KachingPlugIn/Views/{viewName}.cshtml";
        }
    }
}