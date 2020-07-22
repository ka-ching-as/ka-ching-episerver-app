using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Infrastructure;
using Owin;

namespace KachingPlugIn.Web.Owin
{
    public class KachingApiAuthenticationMiddleware : AuthenticationMiddleware<KachingApiAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public KachingApiAuthenticationMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            KachingApiAuthenticationOptions options)
            : base(next, options)
        {
            _logger = app.CreateLogger<KachingApiAuthenticationMiddleware>();

            if (Options.Provider == null)
            {
                Options.Provider = new KachingApiAuthenticationProvider
                {
                    OnValidateIdentity = context =>
                    {
                        if (string.Equals(context.ApiKey, Options.ApiKey, StringComparison.Ordinal))
                        {
                            context.Validate();
                        }

                        return Task.FromResult(0);
                    }
                };
            }
        }

        protected override AuthenticationHandler<KachingApiAuthenticationOptions> CreateHandler()
        {
            return new KachingApiAuthenticationHandler(_logger);
        }
    }
}
