using System.Net.Http;

namespace VchUtils
{
    public static class HttpResponseExtension
    {
        public static void EnsureSucces(this HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }
        
    }
}