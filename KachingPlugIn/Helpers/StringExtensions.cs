using System;

namespace KachingPlugIn.Helpers
{
    public static class StringExtensions
    {
        public static string KachingCompatibleKey(this string key)
        {
            return key.
                ToLower()
                .Replace(' ', '_')
                .Replace('*', '_')
                .Replace('/', '_')
                .Replace('.', '_')
                .Replace('$', '_')
                .Replace('[', '_')
                .Replace(']', '_')
                .Replace('#', '_');
        }

        public static bool IsValidProductsImportUrl(this string url)
        {
            return IsValidUrl(url, "products");
        }

        public static bool IsValidTagsImportUrl(this string url)
        {
            return IsValidUrl(url, "tags");
        }

        public static bool IsValidFoldersImportUrl(this string url)
        {
            return IsValidUrl(url, "folders");
        }

        private static bool IsValidUrl(string url, string path)
        {
            Uri uriResult;
            if (Uri.TryCreate(url, UriKind.Absolute, out uriResult))
            {
                var https = uriResult.Scheme == Uri.UriSchemeHttps;
                var validHost = IsValidHost(uriResult.Host);
                var validPath = uriResult.LocalPath == "/imports/" + path;
                return https && validHost && validPath;
            }
            return false;
        }

        private static bool IsValidHost(string host)
        {
            return host == "us-central1-ka-ching-base-dev.cloudfunctions.net" ||
                host == "us-central1-ka-ching-base-test.cloudfunctions.net" ||
                host == "us-central1-ka-ching-base-staging.cloudfunctions.net" ||
                host == "us-central1-ka-ching-base.cloudfunctions.net";
        }
    }
}