using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Transmitter.Morse;

namespace Transmitter.Db
{
	public static class DbClient
	{
		private static Uri dbUri;
		private const string GetMessageMethod = "search/";

		public static void Init(string dbHref)
			=> dbUri = new Uri(dbHref);

		public static async Task<List<Message>> GetMessagesAsync(string key)
		{
			var request = (HttpWebRequest) WebRequest.Create(dbUri + GetMessageMethod + HttpUtility.UrlEncode(key));

			using (var response = (HttpWebResponse) (await request.GetResponseAsync().ConfigureAwait(false)))
			using (var stream = response.GetResponseStream())
			using (var reader = new StreamReader(stream))
			{
				return JsonConvert.DeserializeObject<List<Message>>(await reader.ReadToEndAsync().ConfigureAwait(false));
			}
		}
	}
}