using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Transmitter.Morse;

namespace Transmitter.Db
{
	public static class DbClient
	{
	    public static Uri DbUri = new Uri("http://192.168.43.81:8000");
	    public const string GetMessageMethod = "search/";
	    public const string PostMessageMethod = "msg";

        public static IEnumerable<Message> GetMessages(string key)
		{
			yield return new Message {Text = "hello, world!", DPM = 120, Frequency = 2000};
			yield return new Message {Text = "RuCTF", DPM = 200, Frequency = 5000};
		}

	    public static async Task<List<Message>> GetMessagesAsync(string key)
	    {
	        var request = (HttpWebRequest)WebRequest.Create(DbUri + GetMessageMethod + key);

            using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                    {
                        return JsonConvert.DeserializeObject<List<Message>>(await reader.ReadToEndAsync());
                    }
	    }

	    public static async Task PostAsync(Message message)
	    {
	        var request = (HttpWebRequest)WebRequest.Create(DbUri + PostMessageMethod);
	        var data = Encoding.ASCII.GetBytes(message.ToJson());

	        request.Method = "POST";
	        request.ContentType = "application/json";
	        request.ContentLength = data.Length;

	        using (var stream = await request.GetRequestStreamAsync())
	        {
	            stream.Write(data, 0, data.Length);
	        }
	        var response = (HttpWebResponse)(await request.GetResponseAsync());

	    }
    }
}