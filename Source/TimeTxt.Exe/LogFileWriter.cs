using System;
using System.IO;
using TimeTxt.Core;

namespace TimeTxt.Exe
{
	internal class LogFileWriter : ILogWriter
	{
		private static readonly object Mutex = new object();

		private readonly string logFile;

		public LogFileWriter(string logFile)
		{
			if (!File.Exists(logFile))
				throw new ArgumentException(string.Format("File '{0}' does not exist.", logFile));
			this.logFile = logFile;
		}

		private static void WriteLine(string logFile, DateTime logTime, string logMessage)
		{
			lock (Mutex)
			{
				using (var writer = new StreamWriter(logFile, true))
				{
					writer.WriteLine("{0}: {1}", logTime, logMessage);
					writer.Flush();
				}
			}
		}

		public void WriteLine()
		{
			WriteLine(logFile, DateTime.Now, string.Empty);
		}

		public void WriteLine(string message)
		{
			WriteLine(logFile, DateTime.Now, message);
		}

		public void WriteLine(string message, object arg0)
		{
			WriteLine(logFile, DateTime.Now, string.Format(message, arg0));
		}

		public void WriteLine(string message, object arg0, object arg1)
		{
			WriteLine(logFile, DateTime.Now, string.Format(message, arg0, arg1));
		}

		public void WriteLine(string message, object arg0, object arg1, object arg2)
		{
			WriteLine(logFile, DateTime.Now, string.Format(message, arg0, arg1, arg2));
		}

		public void WriteLine(string message, params object[] args)
		{
			WriteLine(logFile, DateTime.Now, string.Format(message, args));
		}
	}
}
