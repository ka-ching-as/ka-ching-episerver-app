using System;
using System.Web.Mvc;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Security;
using EPiServer.Shell;
using KachingPlugIn.Configuration;
using KachingPlugIn.Helpers;
using KachingPlugIn.Services;
using KachingPlugIn.Web.KachingPlugIn.ViewModels;
using PlugInArea = EPiServer.PlugIn.PlugInArea;

namespace KachingPlugIn.Web.KachingPlugIn.Controllers
{
    [GuiPlugIn(
        Area = PlugInArea.AdminMenu,
        Url = "/Episerver/KachingPlugIn.Web/KachingPlugIn",
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
            var configuration = KachingConfiguration.Instance;

            var viewModel = new PlugInViewModel();
            viewModel.ProgressViewModel = BuildProgressViewModel();
            viewModel.ProductExportStartButtonDisabled = !configuration.ProductsImportUrl.IsValidProductsImportUrl();
            viewModel.CategoryExportStartButtonDisabled = !configuration.TagsImportUrl.IsValidTagsImportUrl() ||
                                                          !configuration.FoldersImportUrl.IsValidFoldersImportUrl();

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult StartFullProductExport()
        {
            var configuration = KachingConfiguration.Instance;
            _productExport.StartFullProductExport(configuration.ProductsImportUrl);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult StartFullCategoryExport()
        {
            _log.Information("StartFullCategoryExport");
            var configuration = KachingConfiguration.Instance;
            _categoryExport.StartFullCategoryExport();
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

            return PartialView(
                Paths.ToResource("KachingPlugIn.Web", "Views/Progress.cshtml"),
                BuildProgressViewModel());
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
                result.ModelName = _productExport.ModelName;
            }
            else if (_categoryExport.Busy)
            {
                result.Busy = _categoryExport.Busy;
                result.Total = _categoryExport.Total;
                result.Exported = _categoryExport.Uploaded;
                result.Polls = _categoryExport.Polls;
                result.Error = _categoryExport.Error;
                result.ModelName = _categoryExport.ModelName;
            }
            else
            {
                result.Busy = false;
                result.Total = 0;
                result.Exported = 0;
                result.Polls = 0;
                result.Error = false;
                result.Action = string.Empty;
                result.ModelName = string.Empty;
            }
            
            return result;
        }
    }
}