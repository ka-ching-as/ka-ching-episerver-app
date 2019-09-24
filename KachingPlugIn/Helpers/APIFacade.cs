using EPiServer.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace KachingPlugIn.Helpers
{
    public class APIFacade
    {
        private static readonly ILogger _log = LogManager.GetLogger(typeof(APIFacade));

        public static HttpStatusCode Delete(IList<string> ids, string url)
        {
            var deleteUrl = url + "&ids=" + string.Join(",", ids);
            _log.Information("Delete url: " + deleteUrl);

            WebRequest request = WebRequest.Create(deleteUrl);
            request.Method = "DELETE";

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            return response.StatusCode;
        }

        public static HttpStatusCode Post(object model, string url)
        {
            WebRequest request = WebRequest.Create(url);

            request.Method = "POST";
            request.ContentType = "application/json";

            Stream dataStream = request.GetRequestStream();

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            };

            string json = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            });

            _log.Information(json);

            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(json);
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            HttpWebResponse response = request.GetResponse() as HttpWebResponse;
            return response.StatusCode;
        }
    }
}