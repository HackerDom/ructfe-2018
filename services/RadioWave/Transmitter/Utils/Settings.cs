using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Transmitter.Utils
{
	public class Settings
	{
		public static Settings Load()
		{
			var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
			using (var stream = new StreamReader(settingsPath, Encoding.UTF8))
			{
				return JsonConvert.DeserializeObject<Settings>(stream.ReadToEnd());
			}
			
		}

		public ushort Port { get; set; }
	}
}