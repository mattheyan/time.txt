using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using TimeTxt.Core;
using TimeTxt.Exe.Properties;

namespace TimeTxt.Exe
{
	static class Program
	{
		private static readonly object FileWriterMutex = new object();

		static TResult Retry<TResult>(Func<TResult> func)
		{
			var numAttempts = 0;
			Exception lastException = null;

			while (numAttempts < 5)
			{
				numAttempts++;

				try
				{
					return func();
				}
				catch (Exception e)
				{
					lastException = e;
					Thread.Sleep(500);
				}
			}

			throw lastException;
		}

		static FileStream OpenWrite(string fileName)
		{
			return Retry(() => File.OpenWrite(fileName));
		}

		static FileStream OpenRead(string fileName)
		{
			return Retry(() => File.OpenRead(fileName));
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			ITextLogger logger = null;

			try
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (var trayIcon = new TrayIcon())
				{
					logger = CreateLogger();
					trayIcon.UseLogger(logger);

					trayIcon.Display();

					if (string.IsNullOrEmpty(Settings.Default.TargetFile))
					{
						logger.WriteLine("Attempting to find time.txt file...");

						var homeDir = Path.Combine(@"C:\Users", Environment.UserName);
						var dropboxDir = Path.Combine(homeDir, "Dropbox");
						if (!Directory.Exists(dropboxDir))
						{
							logger.WriteLine("ERROR: Dropbox not found!");
							return;
						}

						var timeTxtDropboxFile = Path.Combine(dropboxDir, @"Apps\time.txt\time.txt");
						if (!File.Exists(timeTxtDropboxFile))
						{
							logger.WriteLine("File 'time.txt' not found!");
							return;
						}

						logger.WriteLine("Disovered time.txt file in Dropbox.");
						Settings.Default.TargetFile = timeTxtDropboxFile;
						Settings.Default.Save();
					}

					trayIcon.AddDependent(AutoUpdateFile(Settings.Default.TargetFile, logger));

					// Launch target time.txt file when double-clicked: http://stackoverflow.com/a/9869764.
					trayIcon.DoubleClick += (sender, args) => Process.Start(Settings.Default.TargetFile);

					Application.Run();
				}
			}
			catch (Exception e)
			{
				if (logger != null)
					logger.WriteLine("ERROR: {0}", e.Message);
				else
					EventLog.WriteEntry("time.txt", e.Message, EventLogEntryType.Error);

				throw;
			}
		}

		private static ITextLogger CreateLogger()
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

			var logger = new Logger(logFile);

			if (logFileCreated)
				logger.WriteLine("Log file initialized.");

			return logger;
		}

		private static IDisposable AutoUpdateFile(string file, ITextLogger logger)
		{
			var directoryName = Path.GetDirectoryName(file);
			if (directoryName == null)
				throw new ApplicationException(string.Format("Could not get directory name for file '{0}'.", file));

			var fileName = Path.GetFileName(file);
			if (fileName == null)
				throw new ApplicationException(string.Format("Could not get file name for file '{0}'.", file));

			var watcher = new FileSystemWatcher(directoryName);

			watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
			watcher.Filter = "*.txt";

			DateTime? lastUpdatedTime = null;

			watcher.Changed += (sender, args) =>
			{
				try
				{
					logger.WriteLine("Detected change to file '{0}'.", args.FullPath);

					lock (FileWriterMutex)
					{
						if (args.FullPath.Equals(file, StringComparison.InvariantCultureIgnoreCase))
						{
							if (!lastUpdatedTime.HasValue || File.GetLastWriteTime(file) > lastUpdatedTime)
							{
								var userDataDir = Path.Combine(Path.Combine(@"C:\Users", Environment.UserName), @"AppData\Local\Time.txt");

								var backupsDir = Path.Combine(userDataDir, "Backups");
								if (!Directory.Exists(backupsDir))
									Directory.CreateDirectory(backupsDir);

								var backupFilePath = Path.Combine(backupsDir, DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".backup.txt");
								logger.WriteLine("Backing up file '{0}' to '{1}'...", file, backupFilePath);
								File.Copy(file, backupFilePath);

								logger.WriteLine("Updating file '{0}'...", file);

								bool successful;

								var start = DateTime.Now;

								Stream buffer = null;

								try
								{
									buffer = new MemoryStream();

									using (var fileInputStream = OpenRead(file))
									{
										string currentLine;
										var processor = new UpdateStreamProcessor();
										if (processor.Update(fileInputStream, buffer, true, out currentLine))
											successful = true;
										else
										{
											successful = false;
											processor.WriteRecoveredData(buffer, currentLine, fileInputStream);
										}
									}

									buffer.Seek(0, SeekOrigin.Begin);

									// Clear the text file up front in the event that there is existing
									// text and it is longer than the text that will be written.
									File.WriteAllText(file, string.Empty);

									using (var fileWriter = new StreamWriter(OpenWrite(file)))
									{
										using (var bufferReader = new StreamReader(buffer))
										{
											string line;
											while ((line = bufferReader.ReadLine()) != null)
												fileWriter.WriteLine(line);
										}
									}
								}
								finally
								{
									if (buffer != null)
										buffer.Dispose();
								}

								if (successful)
								{
									var end = DateTime.Now;
									var duration = end - start;

									lastUpdatedTime = File.GetLastWriteTime(file);

									logger.WriteLine("Operation complete: {0}!", duration);
								}
								else
								{
									logger.WriteLine("Operation failed.");
								}
							}
						}
					}
				}
				catch (Exception fwe)
				{
					logger.WriteLine("ERROR: {0}", fwe.Message);
					throw;
				}
			};

			watcher.EnableRaisingEvents = true;

			return watcher;
		}
	}
}
