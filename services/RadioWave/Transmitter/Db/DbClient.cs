using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Transmitter.Morse;

namespace Transmitter.Db
{
	public static class DbClient
	{
		private static Uri dbUri;
		public const string GetMessageMethod = "search/";
		public const string PostMessageMethod = "msg";

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

		public static async Task PostAsync(Message message)
		{
			var request = (HttpWebRequest) WebRequest.Create(dbUri + PostMessageMethod);
			var data = Encoding.ASCII.GetBytes(message.ToJson());

			request.Method = "POST";
			request.ContentType = "application/json";
			request.ContentLength = data.Length;

			using (var stream = await request.GetRequestStreamAsync())
			{
				stream.Write(data, 0, data.Length);
			}

			var response = (HttpWebResponse) (await request.GetResponseAsync());

		}
	}
}