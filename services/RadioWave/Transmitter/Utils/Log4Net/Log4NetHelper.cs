using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace Transmitter.Utils.Log4Net
{
	public static class Log4NetHelper
	{
		public static void Init()
		{
			try
			{
				var repo = LogManager.GetRepository(Assembly.GetEntryAssembly());
				XmlConfigurator.Configure(repo, new FileInfo("log4net.config"));
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine(ex.ToString());
				Environment.Exit(-1);
			}
		}
	}
}