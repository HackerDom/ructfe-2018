using Newtonsoft.Json;

namespace Vch.Core.Helpers
{
    public static class JsonExtensions
    {
        public static string ToJson(this object item)
        {
            return JsonConvert.SerializeObject(item);
        }

        public static TValue FromJSON<TValue>(this string item)
        {
            return JsonConvert.DeserializeObject<TValue>(item);
        }
    }
}