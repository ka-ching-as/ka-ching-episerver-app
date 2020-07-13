using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using KachingPlugIn.EventHandlers;

namespace KachingPlugIn
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class EventInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var eventHandler = context.Locate.Advanced.GetInstance<CatalogKeyEventHandler>();
            eventHandler.Initialize();
        }

        public void Uninitialize(InitializationEngine context)
        {
            var eventHandler = context.Locate.Advanced.GetInstance<CatalogKeyEventHandler>();
            eventHandler.Uninitialize();
        }
    }
}
