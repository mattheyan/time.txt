using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TimeTxt.Core;

namespace TimeTxt.Exe
{
	class Program
	{
		static void Main(string[] args)
		{
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

						var argSplitIndex = argKeyAndValue.IndexOf(":");
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
					var outputFilePath = Path.GetFullPath(outputFileRaw);

					// If the input and output file are the same, read and write separately.
					if (outputFilePath == inputFilePath)
					{
						Stream buffer = null;

						try
						{
							buffer = new MemoryStream();

							using (var fileInputStream = File.OpenRead(inputFilePath))
							{
								new UpdateStreamProcessor().Update(fileInputStream, buffer);
							}

							buffer.Seek(0, SeekOrigin.Begin);

							using (var fileWriter = new StreamWriter(File.OpenWrite(outputFilePath)))
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
							buffer.Dispose();
						}
					}
					// Files are different, so they can be read from and written to at the same time
					else
					{
						using (var fileInputStream = File.OpenRead(inputFilePath))
						{
							using (var fileOutputStream = File.OpenWrite(outputFilePath))
							{
								new UpdateStreamProcessor().Update(fileInputStream, fileOutputStream);
							}
						}
					}
				}
				// No output file was speicifed, so write the output to standard out.
				else
				{
					using (var fileInputStream = File.OpenRead(inputFilePath))
					{
						using (var standardOutputStream = Console.OpenStandardOutput())
						{
							new UpdateStreamProcessor().Update(fileInputStream, standardOutputStream);
						}
					}
				}
			}
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
