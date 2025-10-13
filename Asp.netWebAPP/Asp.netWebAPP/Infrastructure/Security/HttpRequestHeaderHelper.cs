using System.Net.Http.Headers;

namespace Asp.netWebAPP.Infrastructure.Security
{
    public class HttpRequestHeaderHelper
    {

        public static void ApplyDefaultHeaders(
            HttpClient client,
            string accessToken,
            bool includeRequestId = false,
            bool includeTimestamp = false)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("X-CM-ID", "sbx");

            if (includeRequestId)
                client.DefaultRequestHeaders.Add("REQUEST-ID", Guid.NewGuid().ToString());

            if (includeTimestamp)
                client.DefaultRequestHeaders.Add("TIMESTAMP", DateTime.UtcNow.ToString("o"));
        }

    }
}
