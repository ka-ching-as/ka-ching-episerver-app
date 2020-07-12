using System;
using System.Threading.Tasks;

namespace KachingPlugIn.Owin
{
    public class KachingApiAuthenticationProvider
    {
        public KachingApiAuthenticationProvider()
        {
            OnValidateIdentity = context => Task.FromResult<object>(null);
        }

        public Func<KachingApiValidateIdentityContext, Task> OnValidateIdentity { get; set; }

        public virtual Task ValidateIdentity(KachingApiValidateIdentityContext context)
        {
            return OnValidateIdentity(context);
        }
    }
}
