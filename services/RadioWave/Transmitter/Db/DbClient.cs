using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using log4net;
using Newtonsoft.Json;
using Transmitter.Morse;

namespace Transmitter.Db
{
	public static class DbClient
	{
		private static Uri dbUri;
		private const int Timeout = 1000;

		public static void Init(string dbHref)
			=> dbUri = new Uri(dbHref);

		public static async Task<List<Message>> GetMessagesAsync(string key, int timeout = Timeout)
		{
			var sw = Stopwatch.StartNew();
			var result = await GetMessagesInternalAsync(key, timeout).ConfigureAwait(false);
			Log.Info($"{nameof(DbClient)}.{nameof(GetMessagesAsync)}: procesed key '{key}', found {result?.Count} values, elapsed {sw.Elapsed}");
			return result;
		}

		private static async Task<List<Message>> GetMessagesInternalAsync(string key, int timeout)
		{
			var uri = dbUri + HttpUtility.UrlEncode(key);
			Log.Info($"{nameof(DbClient)}.{nameof(GetMessagesAsync)}: send request to '{uri}'");
			var request = WebRequest.Create(uri);
			var responseTask = GetResponseAsync(request);

			using(var cancelSource = new CancellationTokenSource())
			{
				await Task.WhenAny(responseTask, Task.Delay(timeout, cancelSource.Token)).ConfigureAwait(false);
				cancelSource.Cancel();
			}

			if (responseTask.Status == TaskStatus.RanToCompletion)
				return responseTask.Result;

			if (responseTask.Exception != null)
			{
				Log.Error($"{nameof(DbClient)}.{nameof(GetMessagesAsync)}: exception white get data for key '{key}'", responseTask.Exception);
				return null;
			}

			try
			{
				request.Abort();
			}
			catch
			{
				// ignored
			}

			return null;
		}

		private static async Task<List<Message>> GetResponseAsync(WebRequest request)
		{
			using(var response = await request.GetResponseAsync().ConfigureAwait(false))
			using (var stream = response.GetResponseStream())
			{
				if (stream == null)
					return null;

				using (var reader = new StreamReader(stream))
				{
					return JsonConvert.DeserializeObject<List<Message>>(await reader.ReadToEndAsync().ConfigureAwait(false));
				}
			}
		}

		private static readonly ILog Log = LogManager.GetLogger(typeof(DbClient));
	}
}