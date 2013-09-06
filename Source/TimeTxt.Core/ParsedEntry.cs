using System;
using System.Text;

namespace TimeTxt.Core
{
	public class ParsedEntry
	{
		public DateTime Start { get; internal set; }
		public DateTime? End { get; internal set; }
		public string Notes { get; internal set; }

		// Example:12:00a, 12:00p,
		private const int maxTimeSizeWithoutDuration = 15;
		// Example:(12:00) 12:00a, 12:00p,
		private const int maxTimeSizeWithDuration = 23;

		public override string ToString()
		{
			return ToString(false);
		}

		public string ToString(bool prependDuration)
		{
			var builder = new StringBuilder();

			if (prependDuration && End.HasValue)
			{
				var duration = End.Value - Start;
				builder.Append("(");
				builder.Append(duration.ToString("h\\:mm"));
				builder.Append(") ");
			}

			bool startPm;

			if (Start.Hour == 12)
			{
				startPm = true;
				builder.Append("12");
			}
			else if (Start.Hour > 12)
			{
				startPm = true;
				builder.Append(Start.Hour - 12);
			}
			else
			{
				startPm = false;
				builder.Append(Start.Hour);
			}

			if (Start.Minute > 0)
			{
				builder.Append(":");
				builder.Append(Start.Minute.ToString("00"));
			}

			builder.Append(startPm ? "p," : "a,");

			if (End.HasValue)
			{
				builder.Append(" ");

				bool endPm;

				if (End.Value.Hour == 12)
				{
					endPm = true;
					builder.Append("12");
				}
				else if (End.Value.Hour > 12)
				{
					endPm = true;
					builder.Append(End.Value.Hour - 12);
				}
				else
				{
					endPm = false;
					builder.Append(End.Value.Hour);
				}

				if (End.Value.Minute > 0)
				{
					builder.Append(":");
					builder.Append(End.Value.Minute.ToString("00"));
				}

				builder.Append(endPm ? "p," : "a,");
			}

			if (!string.IsNullOrEmpty(Notes))
			{
				var targetLength = prependDuration ? maxTimeSizeWithDuration : maxTimeSizeWithoutDuration;
				while (builder.Length < targetLength)
					builder.Append(" ");
				builder.Append(" ");
				builder.Append(Notes);
			}

			return builder.ToString();
		}
	}
}
