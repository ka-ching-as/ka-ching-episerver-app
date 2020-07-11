using System;

namespace KachingPlugIn.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a key that is safe to import in Ka-ching, by lower-casing the key and replacing certain characters.
        /// </summary>
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

        public static bool IsValidProductRecommendationsImportUrl(this string url)
        {
            return IsValidUrl(url, "recommendations");
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
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
            {
                return false;
            }

            var https = uriResult.Scheme == Uri.UriSchemeHttps;
            var validHost = IsValidHost(uriResult.Host);
            var validPath = uriResult.LocalPath == "/imports/" + path;

            return https && validHost && validPath;
        }

        private static bool IsValidHost(string host)
        {
            switch (host)
            {
                case "us-central1-ka-ching-base-dev.cloudfunctions.net":
                case "us-central1-ka-ching-base-test.cloudfunctions.net":
                case "us-central1-ka-ching-base-staging.cloudfunctions.net":
                case "us-central1-ka-ching-base.cloudfunctions.net":
                    return true;
                default:
                    return false;
            }
        }
    }
}