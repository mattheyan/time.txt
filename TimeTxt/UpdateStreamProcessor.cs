using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TimeTxt
{
	public class UpdateStreamProcessor
	{
		const string defaultDateTimeFormat = "dddd, MMMM dd, yyyy";

		private string dateTimeFormat = defaultDateTimeFormat;

		private bool lastLineWasEmpty;

		private bool currentLineIsEmpty;

		private long? currentTicks;

		private long? totalTicks;

		private List<Func<string, Stream, bool>> lineProcessors;

		public UpdateStreamProcessor()
		{
			lineProcessors = new List<Func<string, Stream, bool>>();
			lineProcessors.Add(ProcessEmptyLine);
			lineProcessors.Add(ProcessDate);
			lineProcessors.Add(ProcessTime);
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

				foreach (var processor in lineProcessors)
				{
					if (processor(line, outputStream))
					{
						processed = true;
						break;
					}
				}

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
			if (totalTicks.HasValue)
			{
				var span = (new DateTime(totalTicks.Value)) - DateTime.MinValue;

				if (!lastLineWasEmpty)
					WriteToStream("", stream);

				WriteToStream("Week: " + span.ToString("h\\:mm"), stream);
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
			if (currentTicks.HasValue)
			{
				var span = (new DateTime(currentTicks.Value)) - DateTime.MinValue;

				if (!lastLineWasEmpty)
					WriteToStream("", stream);

				WriteToStream("Day: " + span.ToString("h\\:mm"), stream);
			}
		}

		private bool ProcessEmptyLine(string line, Stream stream)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				currentLineIsEmpty = true;
				WriteToStream(line, stream);
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
				currentTicks = 0;
				if (!totalTicks.HasValue)
					totalTicks = 0;
				WriteToStream(date.ToString(dateTimeFormat), stream);
				return true;
			}

			return false;
		}

		private bool ProcessTime(string line, Stream stream)
		{
			if (TimeParser.Matches(line))
			{
				//var match = timeRegex.Match(line);
				//return true;
			}

			return false;
		}
	}
}
