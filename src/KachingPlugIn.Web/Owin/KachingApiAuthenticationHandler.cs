using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Infrastructure;

namespace KachingPlugIn.Web.Owin
{
    public class KachingApiAuthenticationHandler : AuthenticationHandler<KachingApiAuthenticationOptions>
    {
        private readonly ILogger _logger;

        public KachingApiAuthenticationHandler(ILogger logger)
        {
            _logger = logger;
        }

        protected override async Task<AuthenticationTicket> AuthenticateCoreAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Options.ApiKey))
                {
                    return null;
                }

                string token = null;
                string headerValue = Request.Headers.Get("Authorization");

                if (!string.IsNullOrEmpty(headerValue) &&
                    headerValue.StartsWith("KachingKey ", StringComparison.OrdinalIgnoreCase))
                {
                    token = headerValue.Substring("KachingKey ".Length).Trim();
                }

                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                var validateIdentityContext = new KachingApiValidateIdentityContext(Context, token);
                if (Options.Provider != null)
                {
                    await Options.Provider.ValidateIdentity(validateIdentityContext);
                }

                if (!validateIdentityContext.IsValidated)
                {
                    return null;
                }

                var identity = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, Options.UserName),
                        new Claim(ClaimTypes.Role, Options.RoleName)
                    },
                    Options.AuthenticationType);

                var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());

                return ticket;
            }
            catch (Exception error)
            {
                _logger.WriteError("Authentication failed", error);

                return null;
            }
        }
    }
}
