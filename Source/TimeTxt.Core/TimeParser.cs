using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeTxt.Core
{
	public static class TimeParser
	{
		//private static readonly Regex timeRegex = new Regex(@"^(\*?\(\d{1,2}\:\d{2}\)\s*)?(\d{1,2}(\:\d{2})?,\s*\d{1,2}(\:\d{2})?,\s*.*)", RegexOptions.Compiled);
		private static readonly Regex timeRegex = new Regex(@"^(?:\*?\(\d{1,2}\:\d{2}\)\s*)?(?:(?<start>\d{1,2}(?:\:\d{2})?)(?:,(?:\s*(?<end>\d{1,2}(?:\:\d{2})?)(?:,(?<notes>.*))?)?)?)\s*$", RegexOptions.Compiled);

		public static bool Matches(string input)
		{
			return timeRegex.IsMatch(input);
		}

		public static ParsedEntry Parse(string input, DateTime day, TimeSpan lastStart)
		{
			var match = timeRegex.Match(input);

			if (!match.Success)
				return null;

			var result = new ParsedEntry();

			var startText = match.Groups["start"].Value;

			int num;
			if (int.TryParse(startText, out num))
			{
				if (lastStart.Hours <= num)
					result.Start = day.AddHours(num);
				else if (lastStart.Hours <= num + 12)
					result.Start = day.AddHours(num + 12);
				else
					throw new InvalidOperationException();
			}
			else
			{
				TimeSpan timeSpan;
				if (TimeSpan.TryParse(startText, out timeSpan))
				{
					if (lastStart.Hours <= timeSpan.Hours)
						result.Start = day.Add(timeSpan);
					else if (lastStart.Hours <= timeSpan.Hours + 12)
						result.Start = day.Add(timeSpan.Add(day.AddHours(12).TimeOfDay));
				}
				else
					throw new NotImplementedException();
			}

			var endText = match.Groups["end"].Value;

			if (!string.IsNullOrWhiteSpace(endText))
			{
				if (int.TryParse(endText, out num))
				{
					if (lastStart.Hours <= num)
						result.End = day.AddHours(num);
					else if (lastStart.Hours <= num + 12)
						result.End = day.AddHours(num + 12);
					else
						throw new InvalidOperationException();
				}
				else
				{
					TimeSpan timeSpan;
					if (TimeSpan.TryParse(endText, out timeSpan))
					{
						if (lastStart.Hours <= timeSpan.Hours)
							result.End = day.Add(timeSpan);
						else if (lastStart.Hours <= timeSpan.Hours + 12)
							result.End = day.Add(timeSpan.Add(day.AddHours(12).TimeOfDay));
					}
					else
						throw new NotImplementedException();
				}
			}

			var notes = match.Groups["notes"].Value;

			if (!string.IsNullOrWhiteSpace(notes))
				result.Notes = notes.Trim();

			return result;
		}
	}
}
