using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TimeTxt.Core.Extensions;

namespace TimeTxt.Core
{
	public static class TimeParser
	{
		private static readonly Regex timeRegex = new Regex(@"^(?:\*?\((?<duration>\d{1,2}(?:\:|\.)\d{2})\)\s*)?(?:(?<start>\d{1,2}(?:\:\d{2})?(?:AM|PM|A|P|am|pm|a|p)?)(?:(?:,(?:\s*(?<end>\d{1,2}(?:\:\d{2})?(?:AM|PM|A|P|am|pm|a|p)?)(?:,(?<notes>.*))?)?)|(?:,(?<notes>.*)))?)\s*$", RegexOptions.Compiled);

		private static readonly Regex timespanDurationRegex = new Regex("^(?<totalHours>\\d{1,2})\\:(?<minutes>\\d{2})$", RegexOptions.Compiled);

		private static readonly Regex decimalDurationRegex = new Regex("^(?<wholeHours>\\d{1,2})\\.(?<minutesFraction>\\d{2})$", RegexOptions.Compiled);

		private static readonly string[] allowedTimeFormats = GetAllowedTimeFormats().ToArray();

		private static IEnumerable<string> GetAllowedTimeFormats()
		{
			foreach (var hourFormat in new[] { "h", "hh" })
				foreach (var minuteFormat in new[] { ":mm", "" })
					foreach (var ampmFormat in new[] { "t", "tt", "" })
						yield return "yyyy/MM/dd " + hourFormat + minuteFormat + ampmFormat;
		}

		public static bool IsTimeFormatAllowed(string format)
		{
			return allowedTimeFormats.Contains(format);
		}

		public static bool Matches(string input)
		{
			return timeRegex.IsMatch(input);
		}

		private static TimeSpan ParseDuration(string text)
		{
			if (timespanDurationRegex.IsMatch(text))
			{
				return TimeSpan.Parse(text);
			}
			if (decimalDurationRegex.IsMatch(text))
			{
				Match decimalDurationMatch = decimalDurationRegex.Match(text);
				int wholeHours = int.Parse(decimalDurationMatch.Groups["wholeHours"].Value);
				int minutesFraction = int.Parse(decimalDurationMatch.Groups["minutesFraction"].Value);
				return new TimeSpan(wholeHours, (int)Math.Round((double)minutesFraction * 60.0), 0);
			}
			throw new FormatException($"Invalid duration '{text}'.");
		}

		public static ParsedEntry Parse(string input, Date day, TimeSpan timeFloor, bool ignoreDuration = false)
		{
			var match = timeRegex.Match(input);
			if (!match.Success)
				return null;

			var result = new ParsedEntry();

			var durationText = match.Groups["duration"].Value;

			if (!ignoreDuration && !string.IsNullOrEmpty(durationText))
				result.Duration = ParseDuration(durationText);

			var startText = match.Groups["start"].Value;

			var startDateTimeText = day.AsDateTime(DateTimeKind.Local).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) + " " + startText.ToUpper();

			DateTime start;
			if (DateTime.TryParseExact(startDateTimeText, allowedTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out start))
			{
				if (start.TimeOfDay.Ticks >= timeFloor.Ticks)
					result.Start = start;
				else
				{
					var startPlus12Hours = start.AddHours(12);

					if (startPlus12Hours.IsOn(day) && startPlus12Hours.Ticks >= timeFloor.Ticks)
						result.Start = startPlus12Hours;
					else
						throw new InvalidOperationException(string.Format("Cannot travel back in time: floor is {0} and given time is {1}.", timeFloor.ToString("hh:mm", CultureInfo.InvariantCulture), startText));
				}
			}
			else
				throw new FormatException(string.Format("\"{0}\" is not a valid time.", startDateTimeText));

			var endText = match.Groups["end"].Value;

			if (!string.IsNullOrWhiteSpace(endText))
			{
				var endDateTimeText = day.AsDateTime(DateTimeKind.Local).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) + " " + endText.ToUpper();

				DateTime end;
				if (DateTime.TryParseExact(endDateTimeText, allowedTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out end))
				{
					if (end.TimeOfDay.Ticks > result.Start.TimeOfDay.Ticks)
						result.End = end;
					else
					{
						var endPlus12Hours = end.AddHours(12);

						if (endPlus12Hours.IsOn(day) && endPlus12Hours.Ticks > result.Start.TimeOfDay.Ticks)
							result.End = endPlus12Hours;
						else
							throw new InvalidOperationException(string.Format("Cannot travel back in time: floor is {0} and given time is {1}.", result.Start.ToString("hh:mm", CultureInfo.InvariantCulture), endText));
					}
				}
				else
					throw new FormatException(string.Format("\"{0}\" is not a valid time.", endDateTimeText));
			}

			var notes = match.Groups["notes"].Value;

			if (!string.IsNullOrWhiteSpace(notes))
				result.Notes = notes.Trim();

			return result;
		}
	}
}
