using Microsoft.Owin.Security;

namespace KachingPlugIn.Owin
{
    public class KachingApiAuthenticationOptions : AuthenticationOptions
    {
        public KachingApiAuthenticationOptions()
            : this(KachingApiDefaults.AuthenticationType)
        {
        }

        public KachingApiAuthenticationOptions(string authenticationType)
            : base(authenticationType)
        {
            RoleName = KachingApiDefaults.RoleName;
            UserName = KachingApiDefaults.UserName;
        }

        public string ApiKey { get; set; }

        public string RoleName { get; set; }

        public string UserName { get; set; }

        public KachingApiAuthenticationProvider Provider { get; set; }
    }
}
