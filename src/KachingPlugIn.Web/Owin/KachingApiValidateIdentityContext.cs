using Microsoft.Owin;
using Microsoft.Owin.Security.Provider;

namespace KachingPlugIn.Web.Owin
{
    public class KachingApiValidateIdentityContext : BaseContext
    {
        public KachingApiValidateIdentityContext(IOwinContext context, string apiKey)
            : base(context)
        {
            ApiKey = apiKey;
        }

        public string ApiKey { get; protected set; }

        public bool IsValidated { get; private set; }

        protected bool HasError { get; set; }

        public void Validate()
        {
            HasError = false;
            IsValidated = true;
        }
    }
}
