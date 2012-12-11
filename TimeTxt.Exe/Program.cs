using System;
using System.Collections.Generic;
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
		}

		private static void ProgramUsage()
		{
			throw new NotImplementedException();
		}

		private static void TaskUsage(string task)
		{
			throw new NotImplementedException();
		}
	}
}
