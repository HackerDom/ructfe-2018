using System.Net.Http;

namespace VchUtils
{
    public static class HttpResponseExtension
    {
        public static void EnsureSucces(this HttpResponseMessage httpResponse, string message)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException(message + " : " + httpResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }
        }
        
    }
}