﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeTxt.Core
{
	public class UpdateStreamProcessor
	{
		const string defaultDateTimeFormat = "dddd, MMMM dd, yyyy";

		private string dateTimeFormat;

		private int? earliestStart;

		private bool lastLineWasEmpty;

		private bool currentLineIsEmpty;

		private DateTime? currentDay;

		private int ignorableLines = 0;

		private int emptyLines = 0;

		private TimeSpan? lastStart;

		private bool dayInEffect = false;

		private List<TimeSpan> daySpans;

		private List<TimeSpan> weekSpans;

		private List<Func<string, StreamWriter, bool>> lineProcessors;

		private List<string> acceptableDateFormatsList;
		private string[] acceptableDateFormats;

		private static readonly string[] defaultMonthDayFormats = new string[] { "M/d", "MM/dd", "M/dd", "MM/d" };

		public UpdateStreamProcessor()
		{
			InitLineProcessors();
			InitDateFormats(defaultDateTimeFormat);
		}

		public UpdateStreamProcessor(string dateTimeFormat)
		{
			InitLineProcessors();
			InitDateFormats(dateTimeFormat);
		}

		public UpdateStreamProcessor(int earliestStart)
		{
			InitLineProcessors();
			InitDateFormats(defaultDateTimeFormat);
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
			this.lineProcessors = new List<Func<string, StreamWriter, bool>>();
			this.lineProcessors.Add(ProcessComment);
			this.lineProcessors.Add(ProcessEmptyLine);
			this.lineProcessors.Add(ProcessDateUnderline);
			this.lineProcessors.Add(ProcessDate);
			this.lineProcessors.Add(ProcessTime);
			this.lineProcessors.Add(ProcessDayTotal);
			this.lineProcessors.Add(ProcessWeekTotal);
		}

		private void InitDateFormats(string dateTimeFormat)
		{
			this.dateTimeFormat = dateTimeFormat;

			this.acceptableDateFormatsList = new List<string>();
			this.acceptableDateFormatsList.AddRange(defaultMonthDayFormats);
			this.acceptableDateFormatsList.AddRange(defaultMonthDayFormats.Select(f => f + "/yy"));
			this.acceptableDateFormatsList.AddRange(defaultMonthDayFormats.Select(f => f + "/yyyy"));
			this.acceptableDateFormatsList.Add(dateTimeFormat);
		}

		private void InitEarliestStart(int earliestStart)
		{
			if (earliestStart < 0 || earliestStart >= 12)
				throw new ArgumentOutOfRangeException();

			this.earliestStart = earliestStart;
		}

		public void Update(Stream inputStream, Stream outputStream)
		{
			//WriteDebug("Starting processing.\r\n=================\r\n", true);

			if (acceptableDateFormats == null)
				acceptableDateFormats = acceptableDateFormatsList.ToArray();

			var reader = new StreamReader(inputStream);
			var writer = new StreamWriter(outputStream);

			string line;
			while ((line = reader.ReadLine()) != null)
			{
				//WriteDebug(line);

				bool processed = false;

				currentLineIsEmpty = false;

				var preIgnorableLines = ignorableLines;

				foreach (var processor in lineProcessors)
				{
					if (processor(line, writer))
					{
						processed = true;
						break;
					}
				}

				if (currentLineIsEmpty)
					emptyLines += 1;
				else
					emptyLines = 0;

				if (ignorableLines == preIgnorableLines)
				{
					//WriteDebug("\tLine was not ignorable, so resetting ignorable lines count.");
					ignorableLines = 0;
				}

				if (!processed)
					throw new ApplicationException("The line \"" + line + "\" could not be processed." + (dayInEffect ? "" : "  No day currently in effect."));

				if (currentLineIsEmpty)
					//WriteDebug("\tLine was considered empty.");

				lastLineWasEmpty = currentLineIsEmpty;
			}

			FinalizeDay(writer);
			FinalizeWeek(writer);

			//WriteDebug("=================\r\nFinished processing.");
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

			if (weekSpans != null)
			{
				if (!lastLineWasEmpty)
				{
					//WriteDebug("\tLast line was not empty, so writing one now.");
					WriteToStream("", writer);
				}

				if (weekSpans.Any())
				{
					var totalWeekSpan = weekSpans.Aggregate((l, r) => l + r);
					var sum = totalWeekSpan.TotalHours.ToString("0") + ":" + totalWeekSpan.Minutes.ToString("00");

					//WriteDebug("\tWriting week value: " + sum + ".");
					WriteToStream("Week: " + sum, writer);
				}
				else
				{
					//WriteDebug("\tNo time to write for the week.");
					WriteToStream("Week: 0:00", writer);
				}
			}
		}

		private bool FinalizeDay(StreamWriter writer)
		{
			//WriteDebug("\tFinalizing day...");

			if (daySpans != null)
			{
				if (!lastLineWasEmpty)
				{
					//WriteDebug("\tLast line was not empty, so writing one now.");
					WriteToStream("", writer);
				}

				if (daySpans.Any())
				{
					var totalDaySpan = daySpans.Aggregate((l, r) => l + r);
					var sum = totalDaySpan.ToString("h\\:mm");

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
				lastLineWasEmpty = false;
				return true;
			}

			return false;
		}

		private bool ProcessComment(string line, StreamWriter writer)
		{
			if (line.StartsWith("#"))
			{
				WriteToStream(line, writer);
				return true;
			}

			return false;
		}

		private bool ProcessEmptyLine(string line, StreamWriter writer)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				//WriteDebug("\tLine is null or whitespace.");
				currentLineIsEmpty = true;
				if (emptyLines == ignorableLines)
				{
					//WriteDebug("\tSo far all ignroable lines are empty lines, so write the whitespace to the response.");
					WriteToStream(line, writer);
				}
				ignorableLines++;
				return true;
			}

			return false;
		}

		private bool ProcessDate(string line, StreamWriter writer)
		{
			DateTime date;
			if (DateTime.TryParseExact(line, acceptableDateFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out date))
			{
				//WriteDebug("\t" + line + " parsed as date " + date.ToString());

				if (FinalizeDay(writer))
				{
					//WriteDebug("\tPrevious day was ended, so writing empty line.");
					WriteToStream("", writer);
				}

				dayInEffect = true;
				currentDay = date;
				daySpans = new List<TimeSpan>();
				lastStart = null;
				if (weekSpans == null)
					weekSpans = new List<TimeSpan>();
				var dateText = date.ToString(dateTimeFormat);
				WriteToStream(dateText, writer);
				var equals = new string(new object[dateText.Length].Select(o => '=').ToArray());
				WriteToStream(equals, writer);
				return true;
			}

			return false;
		}

		private bool ProcessDateUnderline(string line, StreamWriter writer)
		{
			if (dayInEffect)
			{
				var trimmed = line.Trim();
				if (trimmed.Length > 0 && trimmed.Trim(new char[] { '=' }).Length == 0)
				{
					//WriteDebug("\tIgnoring existing date underline.");
					return true;
				}
			}

			return false;
		}

		private bool ProcessTime(string line, StreamWriter writer)
		{
			if (dayInEffect && currentDay.HasValue && TimeParser.Matches(line))
			{
				TimeSpan effectiveStart;
				if (lastStart.HasValue)
					effectiveStart = lastStart.Value;
				else
				{
					TimeSpan defaultStart;
					if (earliestStart.HasValue)
					{
						var midnight = currentDay.Value.TimeOfDay;
						var earliestStartTime = currentDay.Value.AddHours(earliestStart.Value).TimeOfDay;
						defaultStart = midnight.Add(earliestStartTime);
					}
					else
						defaultStart = currentDay.Value.TimeOfDay;

					effectiveStart = defaultStart;
				}

				var parsed = TimeParser.Parse(line, currentDay.Value, effectiveStart);

				//WriteDebug("\tWriting time as \"" + parsed.ToString(true) + "\".");
				WriteToStream(parsed.ToString(true), writer);

				lastStart = parsed.Start.Value.TimeOfDay;

				if (parsed.End.HasValue)
				{
					var duration = parsed.End.Value - parsed.Start.Value;
					daySpans.Add(duration);
					weekSpans.Add(duration);
				}

				return true;
			}

			return false;
		}

		private bool ProcessDayTotal(string line, StreamWriter writer)
		{
			if (dayInEffect && line.StartsWith("Day: "))
			{
				if (lastLineWasEmpty)
				{
					currentLineIsEmpty = true;
					emptyLines--;
				}

				dayInEffect = false;
				ignorableLines++;

				return true;
			}

			return false;
		}

		private bool ProcessWeekTotal(string line, StreamWriter writer)
		{
			if (line.StartsWith("Week: "))
			{
				if (lastLineWasEmpty)
				{
					currentLineIsEmpty = true;
					emptyLines--;
				}

				dayInEffect = false;

				ignorableLines++;

				return true;
			}

			return false;
		}
	}
}
