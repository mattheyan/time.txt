using System;
using System.Collections.Generic;
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

		private string dateTimeFormat = defaultDateTimeFormat;

		private bool lastLineWasEmpty;

		private bool currentLineIsEmpty;

		private DateTime? currentDay;

		private int ignorableLines = 0;

		private int emptyLines = 0;

		private TimeSpan? lastStart;

		private List<TimeSpan> daySpans;

		private List<TimeSpan> weekSpans;

		private List<Func<string, Stream, bool>> lineProcessors;

		public UpdateStreamProcessor()
		{
			lineProcessors = new List<Func<string, Stream, bool>>();
			lineProcessors.Add(ProcessEmptyLine);
			lineProcessors.Add(ProcessDateUnderline);
			lineProcessors.Add(ProcessDate);
			lineProcessors.Add(ProcessTime);
			lineProcessors.Add(ProcessDayTotal);
			lineProcessors.Add(ProcessWeekTotal);
		}

		public UpdateStreamProcessor(string dateTimeFormat)
			: this()
		{
			this.dateTimeFormat = dateTimeFormat;
		}

		public Stream Process(Stream inputStream)
		{
			string line;
			var outputStream = new MemoryStream();
			var reader = new StreamReader(inputStream);
			while ((line = reader.ReadLine()) != null)
			{
				bool processed = false;

				currentLineIsEmpty = false;

				var preIgnorableLines = ignorableLines;

				foreach (var processor in lineProcessors)
				{
					if (processor(line, outputStream))
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
					ignorableLines = 0;

				if (!processed)
					throw new ApplicationException("The line \"" + line + "\" could not be processed.");

				lastLineWasEmpty = currentLineIsEmpty;
			}

			FinalizeDay(outputStream);
			FinalizeWeek(outputStream);

			outputStream.Seek(0, SeekOrigin.Begin);

			return outputStream;
		}

		private void FinalizeWeek(MemoryStream stream)
		{
			if (weekSpans != null)
			{
				if (!lastLineWasEmpty)
					WriteToStream("", stream);

				if (weekSpans.Any())
				{
					var totalWeekSpan = weekSpans.Aggregate((l, r) => l + r);
					WriteToStream("Week: " + totalWeekSpan.ToString("h\\:mm"), stream);
				}
				else
					WriteToStream("Week: 0:00", stream);
			}
		}

		private void WriteToStream(string text, Stream stream)
		{
			var writer = new StreamWriter(stream);
			writer.WriteLine(text);
			writer.Flush();
		}

		private void FinalizeDay(Stream stream)
		{
			if (daySpans != null)
			{
				if (!lastLineWasEmpty)
					WriteToStream("", stream);

				if (daySpans.Any())
				{
					var totalDaySpan = daySpans.Aggregate((l, r) => l + r);
					WriteToStream("Day: " + totalDaySpan.ToString("h\\:mm"), stream);
				}
				else
					WriteToStream("Day: 0:00", stream);

				currentDay = null;
				daySpans = null;
				lastStart = null;
				lastLineWasEmpty = false;
			}
		}

		private bool ProcessEmptyLine(string line, Stream stream)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				currentLineIsEmpty = true;
				if (emptyLines == ignorableLines)
					WriteToStream(line, stream);
				ignorableLines++;
				return true;
			}

			return false;
		}

		private bool ProcessDate(string line, Stream stream)
		{
			DateTime date;
			if (DateTime.TryParse(line, out date))
			{
				FinalizeDay(stream);
				currentDay = date;
				daySpans = new List<TimeSpan>();
				lastStart = null;
				if (weekSpans == null)
					weekSpans = new List<TimeSpan>();
				var dateText = date.ToString(dateTimeFormat);
				WriteToStream(dateText, stream);
				var equals = new string(new object[dateText.Length].Select(o => '=').ToArray());
				WriteToStream(equals, stream);
				return true;
			}

			return false;
		}

		private bool ProcessDateUnderline(string line, Stream stream)
		{
			var trimmed = line.Trim();
			if (trimmed.Length > 0 && trimmed.Trim(new char[] { '=' }).Length == 0)
				return true;

			return false;
		}

		private bool ProcessTime(string line, Stream stream)
		{
			if (currentDay.HasValue && TimeParser.Matches(line))
			{
				var parsed = TimeParser.Parse(line, currentDay.Value, lastStart ?? currentDay.Value.TimeOfDay);
				WriteToStream(parsed.ToString(true), stream);
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

		private bool ProcessDayTotal(string line, Stream stream)
		{
			if (line.StartsWith("Day: "))
			{
				if (lastLineWasEmpty)
				{
					currentLineIsEmpty = true;
					emptyLines--;
				}

				ignorableLines++;

				return true;
			}

			return false;
		}

		private bool ProcessWeekTotal(string line, Stream stream)
		{
			if (line.StartsWith("Week: "))
			{
				if (lastLineWasEmpty)
				{
					currentLineIsEmpty = true;
					emptyLines--;
				}

				ignorableLines++;

				return true;
			}

			return false;
		}
	}
}
