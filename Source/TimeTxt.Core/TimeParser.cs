using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimeTxt.Core
{
	public static class TimeParser
	{
		private static readonly Regex TimeRegex = new Regex(@"^(?:\*?\(\d{1,2}\:\d{2}\)\s*)?(?:(?<start>\d{1,2}(?:\:\d{2})?(?:AM|PM|A|P|am|pm|a|p)?)(?:(?:,(?:\s*(?<end>\d{1,2}(?:\:\d{2})?(?:AM|PM|A|P|am|pm|a|p)?)(?:,(?<notes>.*))?)?)|(?:,(?<notes>.*)))?)\s*$", RegexOptions.Compiled);

		private static readonly string[] AllowedTimeFormats = GetAllowedTimeFormats().ToArray();

		private static IEnumerable<string> GetAllowedTimeFormats()
		{
			foreach (var hourFormat in new[] { "h", "hh" })
				foreach (var minuteFormat in new[] { ":mm", "" })
					foreach (var ampmFormat in new[] { "t", "tt", "" })
						yield return "yyyy/MM/dd " + hourFormat + minuteFormat + ampmFormat;
		}

		public static bool IsTimeFormatAllowed(string format)
		{
			return AllowedTimeFormats.Contains(format);
		}

		public static bool Matches(string input)
		{
			return TimeRegex.IsMatch(input);
		}

		public static ParsedEntry Parse(string input, DateTime day, TimeSpan timeFloor)
		{
			var match = TimeRegex.Match(input);

			if (!match.Success)
				return null;

			var result = new ParsedEntry();

			var startText = match.Groups["start"].Value;

			DateTime start;
			if (DateTime.TryParseExact(day.ToString("yyyy/MM/dd") + " " + startText.ToUpper(), AllowedTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out start))
			{
				if (start.TimeOfDay.Ticks >= timeFloor.Ticks)
					result.Start = start;
				else
				{
					var startPlus12Hours = start.AddHours(12);

					if (startPlus12Hours.Date == day.Date && startPlus12Hours.Ticks >= timeFloor.Ticks)
						result.Start = startPlus12Hours;
					else
						throw new InvalidOperationException();
				}
			}
			else
				throw new InvalidOperationException();

			var endText = match.Groups["end"].Value;

			if (!string.IsNullOrWhiteSpace(endText))
			{
				DateTime end;
				if (DateTime.TryParseExact(day.ToString("yyyy/MM/dd") + " " + endText.ToUpper(), AllowedTimeFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out end))
				{
					if (end.TimeOfDay.Ticks > result.Start.TimeOfDay.Ticks)
						result.End = end;
					else
					{
						var endPlus12Hours = end.AddHours(12);

						if (endPlus12Hours.Date == day.Date && endPlus12Hours.Ticks > result.Start.TimeOfDay.Ticks)
							result.End = endPlus12Hours;
						else
							throw new InvalidOperationException();
					}
				}
				else
					throw new NotImplementedException();
			}

			var notes = match.Groups["notes"].Value;

			if (!string.IsNullOrWhiteSpace(notes))
				result.Notes = notes.Trim();

			return result;
		}
	}
}
