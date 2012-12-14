using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

				var path = args[1];

				using (var inputStream = File.OpenRead(path))
				{
					var outputStream = new UpdateStreamProcessor().Process(inputStream);
					using (var reader = new StreamReader(outputStream))
					{
						string line;
						while ((line = reader.ReadLine()) != null)
						{
							Console.WriteLine(line);
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
