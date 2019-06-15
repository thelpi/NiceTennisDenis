using System.Net;

namespace NiceTennisDenis
{
    /// <summary>
    /// Proceeds to query the API.
    /// </summary>
    internal static class ApiRequester
    {
        /// <summary>
        /// Sends a POST to the WebApi.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <param name="jsonDatas">Datas to send.</param>
        /// <exception cref="WebException">Status code is not 200 !</exception>
        internal static void Post(string relativePath, byte[] jsonDatas = null)
        {
            var request = WebRequest.Create(new System.Uri(new System.Uri(Properties.Settings.Default.webApiUrl), relativePath));
            request.Method = "POST";
            request.Timeout = System.Threading.Timeout.Infinite;
            if (jsonDatas?.Length > 0)
            {
                request.ContentType = "application/json";
                request.ContentLength = jsonDatas.Length;
                using (var dataStream = request.GetRequestStream())
                {
                    dataStream.Write(jsonDatas, 0, jsonDatas.Length);
                }
            }
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new WebException("Status code is not 200 !");
                }
            }
        }

        /// <summary>
        /// Sends a GET to the WebApi.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>A dynamic object.</returns>
        /// <exception cref="WebException">Status code is not 200 !</exception>
        internal static dynamic Get(string relativePath)
        {
            var uri = new System.Uri(Properties.Settings.Default.webApiUrl + relativePath);
            var request = WebRequest.Create(uri);
            request.Method = "GET";
            request.Timeout = System.Threading.Timeout.Infinite;
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new System.Exception("Failure !");
                }
                using (var responseStream = response.GetResponseStream())
                {
                    using (var streamReader = new System.IO.StreamReader(responseStream))
                    {
                        return Newtonsoft.Json.JsonConvert.DeserializeObject(streamReader.ReadToEnd());
                    }
                }
            }
        }
    }
}
