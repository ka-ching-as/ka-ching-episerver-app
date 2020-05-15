using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using KachingPlugIn.EventHandlers;

namespace KachingPlugIn.Intialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class PlugInInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var eventHandler = context.Locate.Advanced.GetInstance<CatalogEventHandler>();
            eventHandler.Initialize();
        }

        public void Uninitialize(InitializationEngine context)
        {
            var eventHandler = context.Locate.Advanced.GetInstance<CatalogEventHandler>();
            eventHandler.Uninitialize();
        }
    }
}