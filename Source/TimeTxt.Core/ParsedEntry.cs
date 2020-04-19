using System;
using System.Text;

namespace TimeTxt.Core
{
	public class ParsedEntry
	{
		public DateTime Start { get; internal set; }
		public DateTime? End { get; internal set; }
		public TimeSpan? Duration { get; internal set; }
		public string Notes { get; internal set; }

		public override string ToString()
		{
			return ToString(null);
		}

		public string ToString(bool prependDuration)
		{
			if (prependDuration)
				return ToString(DurationFormat.TimeSpan);

			return ToString(null);
		}

		public string ToString(DurationFormat? durationFormat)
		{
			var builder = new StringBuilder();

			var targetLength = 0;

			if (durationFormat.HasValue)
			{
				// (##:##)
				targetLength += 7;
				TimeSpan? duration = null;
				if (Duration.HasValue)
					duration = Duration.Value;
				else if (End.HasValue)
					duration = End.Value - Start;
				if (duration.HasValue)
				{
					builder.Append("(");
					builder.Append(TimeFormatter.GetDurationString(duration.Value, durationFormat.Value));
					builder.Append(")");

					while (builder.Length < targetLength)
						builder.Append(" ");

					builder.Append(" ");
					targetLength++;
				}
				else
				{
					// Account for space between duration and start that was not included
					targetLength++;
				}
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

			// ##:##p,
			targetLength += 7;

			if (End.HasValue)
			{
				while (builder.Length < targetLength)
					builder.Append(" ");

				builder.Append(" ");
				targetLength++;

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
			else
			{
				// Account for space between start and end that was not included
				targetLength++;
			}

			// ##:##p,
			targetLength += 7;

			if (!string.IsNullOrEmpty(Notes))
			{
				builder.Append(" ");
				targetLength++;

				while (builder.Length < targetLength)
					builder.Append(" ");

				builder.Append(Notes);
			}

			return builder.ToString();
		}
	}

	public enum DurationFormat
	{
		TimeSpan,
		Decimal
	}
}

