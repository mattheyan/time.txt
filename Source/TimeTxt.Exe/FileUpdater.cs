using System;
using System.IO;
using System.Threading;
using TimeTxt.Core;

namespace TimeTxt.Exe
{
	internal class FileUpdater
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

		internal static bool TryUpdateFile(string file, out DateTime updatedTime)
		{
			try
			{
				var userDataDir = Path.Combine(Path.Combine(@"C:\Users", Environment.UserName), @"AppData\Local\Time.txt");

				var backupsDir = Path.Combine(userDataDir, "Backups");
				if (!Directory.Exists(backupsDir))
					Directory.CreateDirectory(backupsDir);

				var backupFilePath = Path.Combine(backupsDir, DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".backup.txt");
				Services.DefaultLogger.WriteLine("Backing up file '{0}' to '{1}'...", file, backupFilePath);
				File.Copy(file, backupFilePath);

				Services.DefaultLogger.WriteLine("Updating file '{0}'...", file);

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

				if (!successful)
				{
					updatedTime = DateTime.MinValue;
					Services.DefaultLogger.WriteLine("Operation failed.");
					return false;
				}

				var end = DateTime.Now;
				var duration = end - start;

				updatedTime = File.GetLastWriteTime(file);
				Services.DefaultLogger.WriteLine("Operation complete: {0}!", duration);
				return true;
			}
			catch (Exception fwe)
			{
				Services.DefaultLogger.WriteLine("ERROR: {0}", fwe.Message);
				throw;
			}
		}

		internal static IDisposable AutoUpdateFile(string file)
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

			DateTime firstUpdatedTime;
			if (TryUpdateFile(file, out firstUpdatedTime))
				lastUpdatedTime = firstUpdatedTime;

			watcher.Changed += (sender, args) =>
			{
				Services.DefaultLogger.WriteLine("Detected change to file '{0}'.", args.FullPath);

				lock (FileWriterMutex)
				{
					if (args.FullPath.Equals(file, StringComparison.InvariantCultureIgnoreCase))
					{
						if (!lastUpdatedTime.HasValue || File.GetLastWriteTime(file) > lastUpdatedTime)
						{
							DateTime updatedTime;
							if (TryUpdateFile(file, out updatedTime))
								lastUpdatedTime = updatedTime;
						}
					}
				}
			};

			watcher.EnableRaisingEvents = true;

			return watcher;
		}
	}
}
