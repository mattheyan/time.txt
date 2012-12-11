using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeTxt
{
	public static class TimeParser
	{
		//private static readonly Regex timeRegex = new Regex(@"^(\*?\(\d{1,2}\:\d{2}\)\s*)?(\d{1,2}(\:\d{2})?,\s*\d{1,2}(\:\d{2})?,\s*.*)", RegexOptions.Compiled);
		private static readonly Regex timeRegex = new Regex(@"^(\d{1,2}(\:\d{2})?(?:,(?:\s*\d{1,2}(\:\d{2})?(?:,.*)?)?)?)\s*$", RegexOptions.Compiled);

		public static bool Matches(string input)
		{
			return timeRegex.IsMatch(input);
		}
	}
}
