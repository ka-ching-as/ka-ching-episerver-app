using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using KachingPlugIn.EventHandlers;

namespace KachingPlugIn.Intialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class KachingPlugInInitialization : IInitializableHttpModule
    {
        private string _pluginName = "KachingPlugIn";
        private bool _initialized;

        public void Initialize(InitializationEngine context)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            RouteTable.Routes.MapRoute(
                null,
                "modules/" + _pluginName + "/Controllers",
                new { controller = _pluginName, action = "Index" });

            var eventHandler = context.Locate.Advanced.GetInstance<CatalogEventHandler>();
            eventHandler.Initialize();
        }

        public void Uninitialize(InitializationEngine context)
        {
            var eventHandler = context.Locate.Advanced.GetInstance<CatalogEventHandler>();
            eventHandler.Uninitialize();
        }

        public void InitializeHttpEvents(HttpApplication application)
        {
            //Add logic to listen to HTTP events, this method is called multiple times so don't add initialization
            //logic in this method.

            //application.BeginRequest += MyHandler;
        }
    }
}