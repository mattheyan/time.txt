using System;
using System.Text;

namespace TimeTxt.Core
{
	internal class TimeFormatter
	{
		public static string GetDurationString(TimeSpan duration, DurationFormat format)
		{
			var builder = new StringBuilder();

			switch (format)
			{
				case DurationFormat.TimeSpan:
					builder.Append(Math.Floor(duration.TotalHours).ToString("0"));
					builder.Append(":");
					builder.Append(duration.Minutes.ToString("00"));
					break;
				case DurationFormat.Decimal:
				{
					double wholeHours = Math.Floor(duration.TotalHours);
					double minutesFraction = Math.Round((double)duration.Minutes / 60.0, 2);
					builder.Append((wholeHours + minutesFraction).ToString("0.00"));
					break;
				}
			}

			return builder.ToString();
		}
	}
}
