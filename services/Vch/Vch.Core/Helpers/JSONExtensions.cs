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
            var settings = new JsonSerializerSettings();
            settings.Converters.Add(new IPEndPointConverter());
            settings.Formatting = Formatting.Indented;

            return JsonConvert.DeserializeObject<TValue>(item, settings);
        }

    }
}