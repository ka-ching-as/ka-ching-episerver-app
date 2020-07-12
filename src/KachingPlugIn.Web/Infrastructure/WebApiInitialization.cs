using System.Web.Http;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace KachingPlugIn.Web.Infrastructure
{
    [InitializableModule]
    public class WebApiInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            GlobalConfiguration.Configuration.Routes.MapHttpRoute(
                "DefaultKachingRoute",
                "api/kaching/{controller}/{id}",
                new {id = RouteParameter.Optional});
        }
    }
}
