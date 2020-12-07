using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using KachingPlugIn.EventHandlers;
using Mediachase.Commerce.Catalog.Events;
using Mediachase.Commerce.Engine.Events;

namespace KachingPlugIn
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class EventInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            IServiceLocator serviceLocator = context.Locate.Advanced;

            var catalogContentEvents = serviceLocator.GetInstance<CatalogContentEvents>();
            var catalogEvents = serviceLocator.GetInstance<ICatalogEvents>();
            var catalogKeyEventBroadcaster = serviceLocator.GetInstance<CatalogKeyEventBroadcaster>();
            var contentEvents = serviceLocator.GetInstance<IContentEvents>();

            catalogContentEvents.Initialize(catalogKeyEventBroadcaster, catalogEvents, contentEvents);
        }

        public void Uninitialize(InitializationEngine context)
        {
            IServiceLocator serviceLocator = context.Locate.Advanced;

            var catalogContentEvents = serviceLocator.GetInstance<CatalogContentEvents>();
            var catalogEvents = serviceLocator.GetInstance<ICatalogEvents>();
            var catalogKeyEventBroadcaster = serviceLocator.GetInstance<CatalogKeyEventBroadcaster>();
            var contentEvents = serviceLocator.GetInstance<IContentEvents>();

            catalogContentEvents.Uninitialize(catalogKeyEventBroadcaster, catalogEvents, contentEvents);
        }
    }
}
