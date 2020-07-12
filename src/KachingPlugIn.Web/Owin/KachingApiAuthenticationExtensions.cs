using System;
using Microsoft.Owin.Extensions;
using Owin;

namespace KachingPlugIn.Owin
{
    public static class KachingApiAuthenticationExtensions
    {
        public static IAppBuilder UseKachingApiKeyAuthentication(
            this IAppBuilder app,
            KachingApiAuthenticationOptions options)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Use(
                typeof(KachingApiAuthenticationMiddleware),
                app,
                options ?? new KachingApiAuthenticationOptions());
            app.UseStageMarker(PipelineStage.Authenticate);

            return app;
        }
    }
}
