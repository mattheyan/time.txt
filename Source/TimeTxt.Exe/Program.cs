using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using TimeTxt.Core;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	static class Program
	{
		private static ILogWriter CreateLogWriter()
		{
			var userDataDir = Path.Combine(Path.Combine(@"C:\Users", Environment.UserName), @"AppData\Local\Time.txt");
			if (!Directory.Exists(userDataDir))
				Directory.CreateDirectory(userDataDir);

			var logFileCreated = false;
			var logFile = Path.Combine(userDataDir, "log.txt");

			if (!File.Exists(logFile))
			{
				File.Create(logFile);
				logFileCreated = true;
			}

			var logger = new LogFileWriter(logFile);

			if (logFileCreated)
				logger.WriteLine("Log file initialized.");

			return logger;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Services.Locator.UseService(CreateLogWriter());
			FileSearchers.RegisterAll();

			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (var trayIcon = new TrayIcon())
				{
					trayIcon.Display();

					IDisposable currentWatchedFile = null;

					if (!string.IsNullOrEmpty(Settings.Default.TimeTxtFile))
					{
						if (File.Exists(Settings.Default.TimeTxtFile))
						{
							currentWatchedFile = FileUpdater.AutoUpdateFile(Settings.Default.TimeTxtFile);
							trayIcon.AddDependent(currentWatchedFile);
						}
						else
						{
							trayIcon.ShowMessage("File missing!", "Could not find file '{0}'. Either restore the missing file and re-start, or select or create a new file.", Settings.Default.TimeTxtFile);
						}
					}

					trayIcon.FileChanged += (sender, args) =>
					{
						var icon = (TrayIcon) sender;
						if (currentWatchedFile != null)
						{
							currentWatchedFile.Dispose();
							icon.RemoveDependent(currentWatchedFile);
						}

						currentWatchedFile = FileUpdater.AutoUpdateFile(args.FilePath);
						icon.AddDependent(currentWatchedFile);

						Settings.Default.TimeTxtFile = args.FilePath;
						Settings.Default.Save();
					};

					// Launch target time.txt file when double-clicked: http://stackoverflow.com/a/9869764.
					trayIcon.DoubleClick += (sender, args) =>
					{
						if (!string.IsNullOrEmpty(Settings.Default.TimeTxtFile))
							Process.Start(Settings.Default.TimeTxtFile);
					};

					Application.Run();
				}
			}
			catch (Exception e)
			{
				try
				{
					Services.DefaultLogger.WriteLine("ERROR: {0}", e.Message);
				}
				catch
				{
					EventLog.WriteEntry("time.txt", e.Message, EventLogEntryType.Error);
				}

				throw;
			}
		}
	}
}
