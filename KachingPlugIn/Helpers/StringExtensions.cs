using System;
using System.Text;

namespace KachingPlugIn.Helpers
{
    public static class StringExtensions
    {
        /// <summary>
        /// Returns a key that is safe to import in Ka-ching by replacing certain characters.
        /// </summary>
        public static string SanitizeKey(this string key)
        {
            if (key == null)
            {
                return null;
            }

            var sb = new StringBuilder(key);
            sb.Replace("_", "%5F");
            sb.Replace(".", "%2E");
            sb.Replace("$", "%24");
            sb.Replace("#", "%23");
            sb.Replace("[", "%5B");
            sb.Replace("]", "%5D");
            sb.Replace("/", "%2F");
            sb.Replace("*", "%2A");

            return sb.ToString();
        }

        public static string DesanitizeKey(this string key)
        {
            if (key == null)
            {
                return null;
            }

            var sb = new StringBuilder(key);
            sb.Replace("%5F", "_");
            sb.Replace("%2E", ".");
            sb.Replace("%24", "$");
            sb.Replace("%23", "#");
            sb.Replace("%5B", "[");
            sb.Replace("%5D", "]");
            sb.Replace("%2F", "/");
            sb.Replace("%2A", "*");

            return sb.ToString();
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