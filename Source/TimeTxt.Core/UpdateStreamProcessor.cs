using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TimeTxt.Core
{
	public class UpdateStreamProcessor
	{
		private const string DefaultDateTimeFormat = "dddd, MMMM dd, yyyy";

		private static readonly string[] defaultMonthDayFormats = new[] { "M/d", "MM/dd", "M/dd", "MM/d" };

		private static readonly Regex PragmaRegex = new Regex("^#\\!\\s*(?<name>[A-Za-z][A-Za-z0-9_]*)=(?<value>.*)$");

		private string dateTimeFormat;

		private int? earliestStart;

		private Date? currentDay;

		private TimeSpan? lastStart;

		private TimeSpan? lastEnd;

		private bool dayInEffect;

		private List<TimeSpan> daySpans;

		private List<TimeSpan> weekSpans;

		private List<Func<string, bool, StreamWriter, bool>> lineProcessors;

		private List<string> acceptableDateFormatsList;

		private string[] acceptableDateFormats;

		private DurationFormat durationFormat;

		private bool preserveBlankLines;

		private bool ignoreExistingDurations;

		private List<object> pendingEntries = new List<object>();

		private string lastLineWritten;

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
			lineProcessors = new List<Func<string, bool, StreamWriter, bool>>
				{
					ProcessPragma,
					ProcessExclusion,
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

			string rawLine = null;
			var lastLineWasBlank = true;
			lastLineWritten = null;

			try
			{
				while ((rawLine = reader.ReadLine()) != null)
				{
					// Remote whitespace from beginning and end of line
					var line = rawLine.Trim();

					if (string.IsNullOrEmpty(line))
					{
						if (preserveBlankLines)
						{
							if (pendingEntries.Count > 0)
							{
								pendingEntries.Add("");
							}
							else
							{
								lastLineWasBlank = true;
								WriteToStream("", writer);
							}
						}
					}
					else
					{
						//WriteDebug(line);

						if (!lineProcessors.Any(p =>
						{
							try
							{
								string previousLastLineWritten = lastLineWritten;
								bool result = p(line, lastLineWasBlank, writer);
								if (lastLineWritten != previousLastLineWritten)
									lastLineWasBlank = string.IsNullOrEmpty(lastLineWritten);
								return result;
							}
							catch (Exception e)
							{
								if (!gracefulRecovery)
									throw;

								FinalizePendingEntries(null, writer, out lastLineWritten);
								WriteToStream("# " + line, writer);
								WriteToStream("# -> ERROR: " + e.Message, writer);
								return true;
							}
						}))
						{
							if (!gracefulRecovery)
								throw new UpdateException(line, dayInEffect);

							FinalizePendingEntries(null, writer, out lastLineWritten);
							WriteToStream("# " + line, writer);
							WriteToStream("# -> ERROR: Line does not match any expected format.", writer);
							currentLine = line;
						}
					}
				}
			}
			catch (Exception e)
			{
				throw new UpdateException(rawLine, dayInEffect, e);
			}

			if (FinalizeDay(lastLineWasBlank, writer))
				lastLineWasBlank = false;

			if (FinalizeWeek(lastLineWasBlank, writer))
				lastLineWasBlank = false;

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
			lastLineWritten = text;
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

		private bool FinalizeWeek(bool lastLineWasBlank, StreamWriter writer)
		{
			//WriteDebug("\tFinalizing week...");

			if (weekSpans == null)
				return false;

			if (!lastLineWasBlank)
			{
				//WriteDebug("\tWriting empty line before week total.");
				WriteToStream("", writer);
			}

			var totalWeekSpan = (!weekSpans.Any()) ? TimeSpan.Zero : weekSpans.Aggregate((TimeSpan l, TimeSpan r) => l + r);
			var sum = TimeFormatter.GetDurationString(totalWeekSpan, durationFormat);
			WriteToStream("Week: " + sum, writer);
			return true;
		}

		private bool FinalizeDay(bool lastLineWasBlank, StreamWriter writer)
		{
			if (FinalizePendingEntries(null, writer, out string lastFinalizedLine))
			{
				lastLineWasBlank = string.IsNullOrEmpty(lastFinalizedLine);
			}
			else if (daySpans == null)
			{
				return false;
			}
			if (pendingEntries.Count > 0)
			{
				throw new InvalidOperationException("Didn't clear up all pending entries.");
			}
			if (!lastLineWasBlank)
			{
				WriteToStream("", writer);
			}
			TimeSpan totalDaySpan = (!daySpans.Any()) ? TimeSpan.Zero : daySpans.Aggregate((TimeSpan l, TimeSpan r) => l + r);
			string sum = TimeFormatter.GetDurationString(totalDaySpan, durationFormat);
			WriteToStream("Day: " + sum, writer);
			dayInEffect = false;
			currentDay = null;
			daySpans = null;
			lastStart = null;
			lastEnd = null;
			return true;
		}

		private bool FinalizePendingEntries(DateTime? untilTime, StreamWriter writer, out string lastLine)
		{
			int processedCount = 0;
			lastLine = null;
			for (int i = 0; i < pendingEntries.Count; i++)
			{
				object entry = pendingEntries[i];
				bool shouldFinalize;
				if (!untilTime.HasValue)
				{
					shouldFinalize = true;
				}
				else if (entry is ParsedEntry)
				{
					ParsedEntry parsed = (ParsedEntry)entry;
					shouldFinalize = ((parsed.End.HasValue && parsed.End.Value.TimeOfDay <= untilTime.Value.TimeOfDay) ? true : false);
				}
				else if (entry is Tuple<string, DateTime, DateTime?>)
				{
					shouldFinalize = ((i == 0) ? true : false);
				}
				else
				{
					if (!(entry is string))
					{
						throw new Exception(string.Format("Found pending entry of unexpected type {0}.", (entry == null) ? "<NULL>" : ("'" + entry.GetType().FullName + "'")));
					}
					shouldFinalize = ((i == 0) ? true : false);
				}
				if (!shouldFinalize)
				{
					break;
				}
				if (entry is ParsedEntry)
				{
					ParsedEntry parsed2 = (ParsedEntry)entry;
					if (parsed2.End.HasValue && !parsed2.Duration.HasValue)
					{
						parsed2.Duration = GetActualDuration(parsed2.Start, parsed2.End.Value, pendingEntries.Skip(1));
					}
					string entryLine = parsed2.ToString(durationFormat);
					WriteToStream(entryLine, writer);
					lastLine = entryLine;
					if (parsed2.Duration.HasValue)
					{
						daySpans.Add(parsed2.Duration.Value);
						weekSpans.Add(parsed2.Duration.Value);
					}
				}
				else if (entry is Tuple<string, DateTime, DateTime?>)
				{
					Tuple<string, DateTime, DateTime?> exclusion = (Tuple<string, DateTime, DateTime?>)entry;
					WriteToStream(exclusion.Item1, writer);
					lastLine = exclusion.Item1;
				}
				else
				{
					if (!(entry is string))
					{
						throw new Exception(string.Format("Found pending entry of unexpected type {0}.", (entry == null) ? "<NULL>" : ("'" + entry.GetType().FullName + "'")));
					}
					string rawLine = (string)entry;
					WriteToStream(rawLine, writer);
					lastLine = rawLine;
				}
				processedCount++;
				pendingEntries.RemoveAt(i--);
			}
			return processedCount > 0;
		}

		private TimeSpan GetActualDuration(DateTime start, DateTime end, IEnumerable<object> followingEntries)
		{
			TimeSpan duration = end - start;
			List<Tuple<DateTime, DateTime>> exclusions = new List<Tuple<DateTime, DateTime>>();
			DateTime? currentExclusionStart = null;
			DateTime? currentExclusionEnd = null;
			foreach (object following in followingEntries)
			{
				Tuple<DateTime, DateTime> potentialExclusion = null;
				if (following is ParsedEntry)
				{
					ParsedEntry followingEntry = (ParsedEntry)following;
					if (followingEntry.End.HasValue)
					{
						potentialExclusion = new Tuple<DateTime, DateTime>(followingEntry.Start, followingEntry.End.Value);
					}
				}
				else if (following is Tuple<string, DateTime, DateTime?>)
				{
					Tuple<string, DateTime, DateTime?> exclusionLine = (Tuple<string, DateTime, DateTime?>)following;
					if (exclusionLine.Item3.HasValue)
					{
						potentialExclusion = new Tuple<DateTime, DateTime>(exclusionLine.Item2, exclusionLine.Item3.Value);
					}
				}
				if (potentialExclusion != null && potentialExclusion.Item1 >= start && potentialExclusion.Item1 < end && (!currentExclusionEnd.HasValue || potentialExclusion.Item1 >= currentExclusionEnd.Value))
				{
					if (currentExclusionStart.HasValue && currentExclusionEnd.HasValue)
					{
						exclusions.Add(new Tuple<DateTime, DateTime>(currentExclusionStart.Value, currentExclusionEnd.Value));
						currentExclusionStart = null;
						currentExclusionEnd = null;
					}
					if (!currentExclusionStart.HasValue || potentialExclusion.Item1 < currentExclusionStart.Value)
					{
						currentExclusionStart = potentialExclusion.Item1;
					}
					if (!currentExclusionEnd.HasValue || potentialExclusion.Item2 < currentExclusionEnd.Value)
					{
						currentExclusionEnd = ((!(potentialExclusion.Item2 >= end)) ? new DateTime?(potentialExclusion.Item2) : new DateTime?(end));
					}
				}
			}
			if (currentExclusionStart.HasValue && currentExclusionEnd.HasValue)
			{
				exclusions.Add(new Tuple<DateTime, DateTime>(currentExclusionStart.Value, currentExclusionEnd.Value));
			}
			foreach (Tuple<DateTime, DateTime> exclusion in exclusions)
			{
				duration -= exclusion.Item2 - exclusion.Item1;
			}
			return duration;
		}

		private bool ProcessPragma(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!line.StartsWith("#!"))
			{
				return false;
			}
			Match pragmaMatch = PragmaRegex.Match(line);
			if (pragmaMatch.Success)
			{
				string pragmaName = pragmaMatch.Groups["name"].Value;
				string pragmaValue = pragmaMatch.Groups["value"].Value;
				if (pragmaName.Equals("preserveBlankLines", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!bool.TryParse(pragmaValue, out preserveBlankLines))
					{
						WriteToStream("# -> WARNING: Couldn't parse '" + pragmaValue + "' as boolean", writer);
					}
				}
				else if (pragmaName.Equals("ignoreExistingDurations", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!bool.TryParse(pragmaValue, out ignoreExistingDurations))
					{
						WriteToStream("# -> WARNING: Couldn't parse '" + pragmaValue + "' as boolean", writer);
					}
				}
				else if (pragmaName.Equals("durationFormat", StringComparison.CurrentCultureIgnoreCase))
				{
					if (!Enum.TryParse(pragmaValue, ignoreCase: true, out durationFormat))
					{
						WriteToStream("# -> WARNING: Couldn't parse '" + pragmaValue + "' as duration format", writer);
					}
				}
				else
				{
					WriteToStream("# -> WARNING: Unknown pragma '" + pragmaName + "'", writer);
				}
			}
			else
			{
				WriteToStream("# -> WARNING: Invalid pragma format", writer);
			}
			WriteToStream(line, writer);
			return true;
		}

		private bool ProcessExclusion(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!line.StartsWith("#-- "))
			{
				return false;
			}
			string exclusionText = line.Substring(4);
			if (!dayInEffect || !currentDay.HasValue || !TimeParser.Matches(exclusionText))
			{
				WriteToStream(line, writer);
				WriteToStream("# -> WARNING: Invalid exclusion line", writer);
				return false;
			}
			TimeSpan effectiveStart = GetEffectiveStart();
			ParsedEntry exclusion = TimeParser.Parse(exclusionText, currentDay.Value, effectiveStart, ignoreExistingDurations);
			pendingEntries.Add(new Tuple<string, DateTime, DateTime?>(line, exclusion.Start, exclusion.End));
			return true;
		}

		private bool ProcessComment(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!line.StartsWith("#"))
			{
				return false;
			}
			if (pendingEntries.Count > 0)
			{
				pendingEntries.Add(line);
			}
			else
			{
				WriteToStream(line, writer);
			}
			return true;
		}

		private bool ProcessDate(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!DateTime.TryParseExact(line, acceptableDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime dateTime))
			{
				return false;
			}
			if (FinalizeDay(lastLineWasBlank, writer))
			{
				lastLineWasBlank = false;
			}
			if (!lastLineWasBlank)
			{
				WriteToStream("", writer);
			}
			dayInEffect = true;
			currentDay = new Date(dateTime);
			daySpans = new List<TimeSpan>();
			lastStart = null;
			lastEnd = null;
			if (weekSpans == null)
			{
				weekSpans = new List<TimeSpan>();
			}
			string dateText = dateTime.ToString(dateTimeFormat);
			WriteToStream(dateText, writer);
			string equals = new string(new object[dateText.Length].Select((object o) => '=').ToArray());
			WriteToStream(equals, writer);
			return true;
		}

		private bool ProcessDateUnderline(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!dayInEffect)
			{
				return false;
			}
			string trimmed = line.Trim();
			if (trimmed.Length > 0 && trimmed.Trim('=').Length == 0)
			{
				return true;
			}
			return false;
		}

		private TimeSpan GetEffectiveStart()
		{
			if (lastStart.HasValue)
			{
				return lastStart.Value;
			}
			if (earliestStart.HasValue)
			{
				TimeSpan midnight = currentDay.Value.LocalDate.TimeOfDay;
				TimeSpan earliestStartTime = currentDay.Value.LocalDate.AddHours(earliestStart.Value).TimeOfDay;
				return midnight.Add(earliestStartTime);
			}
			return currentDay.Value.LocalDate.TimeOfDay;
		}

		private bool ProcessTime(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!dayInEffect || !currentDay.HasValue || !TimeParser.Matches(line))
			{
				return false;
			}
			TimeSpan effectiveStart = GetEffectiveStart();
			ParsedEntry parsed = TimeParser.Parse(line, currentDay.Value, effectiveStart, ignoreExistingDurations);
			if (pendingEntries.Count > 0 && FinalizePendingEntries(parsed.Start, writer, out string lastFinalizedLine))
			{
				lastLineWasBlank = string.IsNullOrEmpty(lastFinalizedLine);
			}
			if (pendingEntries.Count > 0 || (parsed.End.HasValue && !parsed.Duration.HasValue))
			{
				pendingEntries.Add(parsed);
			}
			else
			{
				pendingEntries.Add(parsed);
				if (FinalizePendingEntries(null, writer, out string lastFinalizedLine2))
				{
					lastLineWasBlank = string.IsNullOrEmpty(lastFinalizedLine2);
				}
			}
			lastStart = parsed.Start.TimeOfDay;
			if (parsed.End.HasValue)
			{
				lastEnd = parsed.End.Value.TimeOfDay;
			}
			return true;
		}

		private bool ProcessDayTotal(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!dayInEffect || !line.StartsWith("Day: "))
			{
				return false;
			}
			if (pendingEntries.Count > 0 && FinalizePendingEntries(null, writer, out string lastFinalizedLine))
			{
				lastLineWasBlank = string.IsNullOrEmpty(lastFinalizedLine);
			}
			if (FinalizeDay(lastLineWasBlank, writer))
			{
				lastLineWasBlank = false;
			}
			dayInEffect = false;
			return true;
		}

		private bool ProcessWeekTotal(string line, bool lastLineWasBlank, StreamWriter writer)
		{
			if (!line.StartsWith("Week: "))
			{
				return false;
			}
			if (pendingEntries.Count > 0 && FinalizePendingEntries(null, writer, out string lastFinalizedLine))
			{
				lastLineWasBlank = string.IsNullOrEmpty(lastFinalizedLine);
			}
			dayInEffect = false;
			return true;
		}

		public void WriteRecoveredData(Stream outputStream, string currentLine, Stream inputStream)
		{
			StreamReader reader = new StreamReader(inputStream);
			StreamWriter writer = new StreamWriter(outputStream);
			string line = currentLine;
			do
			{
				writer.WriteLine(line);
			}
			while ((line = reader.ReadLine()) != null);
		}
	}
}
