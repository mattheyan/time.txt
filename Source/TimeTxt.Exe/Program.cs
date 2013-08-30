using System;
using System.IO;
using System.Linq;
using TimeTxt.Core;

namespace TimeTxt.Exe
{
	class Program
	{
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

		static void Main(string[] args)
		{
			bool? successful = null;

			// Display usage information
			if (args.Length == 0 || args[0] == "/?" || args[1] == "/?")
			{
				if (args.Length >= 2 && (args[0] == "/?" || args[1] == "/?"))
					TaskUsage(args[1]);
				else
					ProgramUsage();

				Environment.Exit(args.Length >= 1 && (args[0] == "/?" || args[1] == "/?") ? 0 : -1);
			}

			if (args[0].Equals("update", StringComparison.CurrentCultureIgnoreCase))
			{
				if (args.Length < 2)
				{
					TaskUsage("update");
					Environment.Exit(args.Length >= 1 && (args[0] == "/?" || args[1] == "/?") ? 0 : -1);
				}

				bool launchDebugger = false;

				string backupFilePath = null;

				var inputFileRaw = args[1];
				var inputFilePath = Path.GetFullPath(inputFileRaw);

				string outputFileRaw = null;

				if (args.Length > 2)
				{
					foreach (string arg in args.Skip(2))
					{
						if (!arg.StartsWith("/"))
							throw new ArgumentException("Invalid option \"" + arg + "\".");

						string argKey;
						string argValue;
						var argKeyAndValue = arg.Trim('/');

						var argSplitIndex = argKeyAndValue.IndexOf(":", StringComparison.Ordinal);
						if (argSplitIndex > 0)
						{
							argKey = argKeyAndValue.Substring(0, argSplitIndex);
							argValue = argKeyAndValue.Substring(argSplitIndex + 1);
						}
						else
						{
							argKey = argKeyAndValue;
							argValue = null;
						}

						if (argKey == "out")
							outputFileRaw = argValue;
						else if (argKey == "backup")
						{
							if (!string.IsNullOrEmpty(argValue))
							{
								backupFilePath = Path.GetFullPath(argValue);

								// If backup path is a directory, generate a file with timestamp in that directory.
								if (Directory.Exists(backupFilePath))
									backupFilePath = Path.Combine(argValue, Path.GetFileName(inputFilePath) + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".backup.txt");
							}
							else
							{
								// If no value is specified, generate a file with timestamp in the same directory as the input file.
								backupFilePath = inputFilePath + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".backup.txt";
							}
						}
						else if (argKey == "launch")
							launchDebugger = true;
						else
							throw new ArgumentException("Unknown option \"" + argKey + "\".");
					}
				}

				if (launchDebugger)
					System.Diagnostics.Debugger.Launch();

				if (!string.IsNullOrEmpty(backupFilePath))
					File.Copy(inputFilePath, backupFilePath);

				if (!string.IsNullOrEmpty(outputFileRaw))
				{
					var start = DateTime.Now;

					var outputFilePath = Path.GetFullPath(outputFileRaw);

					// If the input and output file are the same, read and write separately.
					if (outputFilePath == inputFilePath)
					{
						Stream buffer = null;

						try
						{
							buffer = new MemoryStream();

							using (var fileInputStream = OpenRead(inputFilePath))
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
							File.WriteAllText(outputFilePath, String.Empty);

							using (var fileWriter = new StreamWriter(OpenWrite(outputFilePath)))
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
					}
					// Files are different, so they can be read from and written to at the same time
					else
					{
						// Clear the text file up front in the event that there is existing
						// text and it is longer than the text that will be written.
						File.WriteAllText(outputFilePath, String.Empty);

						using (var fileInputStream = OpenRead(inputFilePath))
						{
							using (var fileOutputStream = OpenWrite(outputFilePath))
							{
								string currentLine;
								var processor = new UpdateStreamProcessor();
								if (processor.Update(fileInputStream, fileOutputStream, true, out currentLine))
									successful = true;
								else
								{
									successful = false;
									processor.WriteRecoveredData(fileOutputStream, currentLine, fileInputStream);
								}
							}
						}
					}

					var end = DateTime.Now;
					var duration = end - start;
					Console.WriteLine(duration.ToString());
				}
				// No output file was speicifed, so write the output to standard out.
				else
				{
					using (var fileInputStream = OpenRead(inputFilePath))
					{
						using (var standardOutputStream = Console.OpenStandardOutput())
						{
							string currentLine;
							var processor = new UpdateStreamProcessor();
							if (processor.Update(fileInputStream, standardOutputStream, true, out currentLine))
								successful = true;
							else
							{
								successful = false;
								processor.WriteRecoveredData(standardOutputStream, currentLine, fileInputStream);
							}
						}
					}
				}
			}

			if (successful.HasValue && !successful.Value)
				Environment.Exit(-1);
		}

		private static void ProgramUsage()
		{
			Console.WriteLine("timetxt /? update");
		}

		private static void TaskUsage(string task)
		{
			if (task.Equals("update", StringComparison.CurrentCultureIgnoreCase))
			{
				Console.WriteLine("timetxt update .\\path\\to\\time.txt");
			}
			else
			{
				throw new NotImplementedException();
			}
		}
	}
}
