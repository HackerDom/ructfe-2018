using Newtonsoft.Json;

namespace Vch.Core.Helpers
{
    public static class JsonExtensions
    {
        public static string ToJson(this object item)
        {
            var settings = new JsonSerializerSettings();
            //settings.Converters.Add(new IPEndPointConverter());
            //settings.Converters.Add(new IPAddressConverter());
            //settings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(item, settings);
        }

        public static TValue FromJSON<TValue>(this string item, bool trimQuotes = false)
        {
            var settings = new JsonSerializerSettings();
            //settings.Converters.Add(new IPEndPointConverter());
            //settings.Converters.Add(new IPAddressConverter());
            //settings.Formatting = Formatting.Indented;

            var source = item;

            if (trimQuotes && item.StartsWith(@"""") && item.EndsWith(@""""))
            {
                source = item.Substring(1, item.Length - 2);
            }

            return JsonConvert.DeserializeObject<TValue>(source, settings);
        }

    }
}