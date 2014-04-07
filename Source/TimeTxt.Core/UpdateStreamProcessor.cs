using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace TimeTxt.Core
{
	public class UpdateStreamProcessor
	{
		private const string DefaultDateTimeFormat = "dddd, MMMM dd, yyyy";

		private static readonly string[] defaultMonthDayFormats = new[] { "M/d", "MM/dd", "M/dd", "MM/d" };

		private string dateTimeFormat;

		private int? earliestStart;

		private Date? currentDay;

		private TimeSpan? lastStart;

		private bool dayInEffect;

		private List<TimeSpan> daySpans;

		private List<TimeSpan> weekSpans;

		private List<Func<string, StreamWriter, bool>> lineProcessors;

		private List<string> acceptableDateFormatsList;

		private string[] acceptableDateFormats;

		public UpdateStreamProcessor()
		{
			InitLineProcessors();
			InitDateFormats(DefaultDateTimeFormat);
		}

		public UpdateStreamProcessor(string dateTimeFormat)
		{
			InitLineProcessors();
			InitDateFormats(dateTimeFormat);
		}

		public UpdateStreamProcessor(int earliestStart)
		{
			InitLineProcessors();
			InitDateFormats(DefaultDateTimeFormat);
			InitEarliestStart(earliestStart);
		}

		public UpdateStreamProcessor(string dateTimeFormat, int earliestStart)
		{
			InitLineProcessors();
			InitDateFormats(dateTimeFormat);
			InitEarliestStart(earliestStart);
		}

		private void InitLineProcessors()
		{
			lineProcessors = new List<Func<string, StreamWriter, bool>>
				{
					ProcessComment,
					ProcessDateUnderline,
					ProcessDate,
					ProcessTime,
					ProcessDayTotal,
					ProcessWeekTotal
				};
		}

		private void InitDateFormats(string format)
		{
			dateTimeFormat = format;

			acceptableDateFormatsList = new List<string>();
			acceptableDateFormatsList.AddRange(defaultMonthDayFormats);
			acceptableDateFormatsList.AddRange(defaultMonthDayFormats.Select(f => f + "/yy"));
			acceptableDateFormatsList.AddRange(defaultMonthDayFormats.Select(f => f + "/yyyy"));
			acceptableDateFormatsList.Add(format);
		}

		private void InitEarliestStart(int hour)
		{
			if (hour < 0 || hour >= 12)
				throw new ArgumentOutOfRangeException();

			earliestStart = hour;
		}

		public bool Update(Stream inputStream, Stream outputStream, bool gracefulRecovery, out string currentLine)
		{
			//WriteDebug("Starting processing.\r\n=================\r\n", true);

			if (acceptableDateFormats == null)
				acceptableDateFormats = acceptableDateFormatsList.ToArray();

			var reader = new StreamReader(inputStream);
			var writer = new StreamWriter(outputStream);

			string line = null;

			try
			{
				while ((line = reader.ReadLine()) != null)
				{
					// Remote whitespace from beginning and end of line
					line = line.Trim();

					if (string.IsNullOrEmpty(line))
						continue;

					//WriteDebug(line);

					// Look for the first line processor to use the line.
					if (!lineProcessors.Any(p =>
					{
						try
						{
							return p(line, writer);
						}
						catch (Exception e)
						{
							if (!gracefulRecovery)
								throw;

							WriteToStream("# -> ERROR: " + e.Message, writer);
							WriteToStream(line, writer);
							return true;
						}
					}))
					{
						if (!gracefulRecovery)
							throw new UpdateException(line, dayInEffect);

						currentLine = line;
						return false;
					}
				}
			}
			catch (Exception e)
			{
				throw new UpdateException(line, dayInEffect, e);
			}

			FinalizeDay(writer);
			FinalizeWeek(writer);

			//WriteDebug("=================\r\nFinished processing.");

			currentLine = null;
			return true;
		}

		public class UpdateException : Exception
		{
			public string Line { get; set; }

			public bool DayInEffect { get; set; }

			public UpdateException(string line, bool dayInEffect)
			{
				Line = line;
				DayInEffect = dayInEffect;
			}

			public UpdateException(string line, bool dayInEffect, Exception innerException)
				: base(GetMessage(line, dayInEffect, innerException), innerException)
			{
				Line = line;
				DayInEffect = dayInEffect;
			}

			private static string GetMessage(string line, bool dayInEffect, Exception innerException)
			{
				var messageBuilder = new StringBuilder();
				messageBuilder.AppendFormat("The line \"{0}\" could not be processed.", line);
				if (!dayInEffect)
					messageBuilder.Append(" No day currently in effect.");
				if (innerException != null)
					messageBuilder.AppendFormat(" Error: \"{0}\".", innerException.Message);
				return messageBuilder.ToString();
			}

			public override string Message
			{
				get { return GetMessage(Line, DayInEffect, InnerException); }
			}
		}

		private void WriteToStream(string text, StreamWriter writer)
		{
			//WriteDebug("\tOUTPUT: " + text);

			writer.WriteLine(text);
			writer.Flush();
		}

		//private void WriteDebug(string text)
		//{
		//	WriteDebug(text, false);
		//}

		//private void WriteDebug(string text, bool emptyFirst)
		//{
		//	if (emptyFirst)
		//		File.WriteAllText("log.txt", "");

		//	using (var writer = File.AppendText("log.txt"))
		//	{
		//		writer.WriteLine(text);
		//	}
		//}

		private void FinalizeWeek(StreamWriter writer)
		{
			//WriteDebug("\tFinalizing week...");

			if (weekSpans == null)
				return;

			//WriteDebug("\tWriting empty line before week total.");
			WriteToStream("", writer);

			if (weekSpans.Any())
			{
				var totalWeekSpan = weekSpans.Aggregate((l, r) => l + r);
				var sum = Math.Floor(totalWeekSpan.TotalHours).ToString("0") + ":" + totalWeekSpan.Minutes.ToString("00");

				//WriteDebug("\tWriting week value: " + sum + ".");
				WriteToStream("Week: " + sum, writer);
			}
			else
			{
				//WriteDebug("\tNo time to write for the week.");
				WriteToStream("Week: 0:00", writer);
			}
		}

		private bool FinalizeDay(StreamWriter writer)
		{
			//WriteDebug("\tFinalizing day...");

			if (daySpans == null)
				return false;

			//WriteDebug("\tWriting empty line before day total.");
			WriteToStream("", writer);

			if (daySpans.Any())
			{
				var totalDaySpan = daySpans.Aggregate((l, r) => l + r);
				var sum = totalDaySpan.ToString("h\\:mm", CultureInfo.InvariantCulture);

				//WriteDebug("\tWriting day value: " + sum + ".");
				WriteToStream("Day: " + sum, writer);
			}
			else
			{
				//WriteDebug("\tNo time to write for the day.");
				WriteToStream("Day: 0:00", writer);
			}

			dayInEffect = false;
			currentDay = null;
			daySpans = null;
			lastStart = null;
			return true;
		}

		private bool ProcessComment(string line, StreamWriter writer)
		{
			if (!line.StartsWith("#"))
				return false;

			WriteToStream(line, writer);
			return true;
		}

		private bool ProcessDate(string line, StreamWriter writer)
		{
			DateTime dateTime;

			if (!DateTime.TryParseExact(line, acceptableDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
				return false;

			//WriteDebug("\t" + line + " parsed as date " + date.ToString());

			if (FinalizeDay(writer))
			{
				//WriteDebug("\tPrevious day was ended, so writing empty line.");
				WriteToStream("", writer);
			}

			dayInEffect = true;
			currentDay = new Date(dateTime);
			daySpans = new List<TimeSpan>();
			lastStart = null;
			if (weekSpans == null)
				weekSpans = new List<TimeSpan>();
			var dateText = dateTime.ToString(dateTimeFormat);
			WriteToStream(dateText, writer);
			var equals = new string(new object[dateText.Length].Select(o => '=').ToArray());
			WriteToStream(@equals, writer);
			return true;
		}

		private bool ProcessDateUnderline(string line, StreamWriter writer)
		{
			if (!dayInEffect)
				return false;

			var trimmed = line.Trim();

			if (trimmed.Length > 0 && trimmed.Trim(new[] { '=' }).Length == 0)
			{
				//WriteDebug("\tIgnoring existing date underline.");
				return true;
			}

			return false;
		}

		private bool ProcessTime(string line, StreamWriter writer)
		{
			if (!dayInEffect || !currentDay.HasValue || !TimeParser.Matches(line))
				return false;

			TimeSpan effectiveStart;

			if (lastStart.HasValue)
				effectiveStart = lastStart.Value;
			else
			{
				TimeSpan defaultStart;
				if (earliestStart.HasValue)
				{
					var midnight = currentDay.Value.LocalDate.TimeOfDay;
					var earliestStartTime = currentDay.Value.LocalDate.AddHours(earliestStart.Value).TimeOfDay;
					defaultStart = midnight.Add(earliestStartTime);
				}
				else
					defaultStart = currentDay.Value.LocalDate.TimeOfDay;

				effectiveStart = defaultStart;
			}

			var parsed = TimeParser.Parse(line, currentDay.Value, effectiveStart);

			//WriteDebug("\tWriting time as \"" + parsed.ToString(true) + "\".");
			WriteToStream(parsed.ToString(true), writer);

			lastStart = parsed.Start.TimeOfDay;

			if (parsed.End.HasValue)
			{
				var duration = parsed.End.Value - parsed.Start;
				daySpans.Add(duration);
				weekSpans.Add(duration);
			}

			return true;
		}

		private bool ProcessDayTotal(string line, StreamWriter writer)
		{
			if (!dayInEffect || !line.StartsWith("Day: "))
				return false;

			dayInEffect = false;
			return true;
		}

		private bool ProcessWeekTotal(string line, StreamWriter writer)
		{
			if (!line.StartsWith("Week: "))
				return false;

			dayInEffect = false;
			return true;
		}

		public void WriteRecoveredData(Stream outputStream, string currentLine, Stream inputStream)
		{
			var reader = new StreamReader(inputStream);
			var writer = new StreamWriter(outputStream);

			var line = currentLine;

			do
			{
				writer.WriteLine(line);
			} while ((line = reader.ReadLine()) != null);
		}
	}
}
