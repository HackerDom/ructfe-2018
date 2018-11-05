using System;
using log4net;
using vtortola.WebSockets;

namespace Transmitter.Utils.Log4Net
{
	public class ILogWrapper : ILogger
	{
		private readonly ILog log;

		public ILogWrapper(ILog log)
		{
			this.log = log;
		}

		public void Debug(string message, Exception error = null)
			=> log.Debug(message, error);

		public void Warning(string message, Exception error = null)
			=> log.Warn(message, error);

		public void Error(string message, Exception error = null)
			=> log.Error(message, error);

		public bool IsDebugEnabled
			=> log.IsDebugEnabled;

		public bool IsWarningEnabled
			=> log.IsWarnEnabled;

		public bool IsErrorEnabled
			=> log.IsErrorEnabled;
	}
}