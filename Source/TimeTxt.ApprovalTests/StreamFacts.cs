using ApprovalTests;
using ApprovalTests.Reporters;
using System.IO;
using TimeTxt.Core;
using Xunit;

namespace TimeTxt.ApprovalTests
{
	[UseReporter(typeof(DiffReporter))]
	public class StreamFact
	{
		public class WhenTheInputIsBlank : StreamFact
		{
			[Fact]
			public void TheOutputIsBlank()
			{
				Approvals.Verify(Update(""));
			}

		}

		public class WhenTheInputIsWhitespace : StreamFact
		{
			[Fact]
			public void ItIsPreserved()
			{
				Approvals.Verify(Update("\r\n  \r"));
			}
		}

		public class WhenADateIsStarted : StreamFact
		{
			[Fact]
			public void ItIsReformattedFromAMinimalDate()
			{
				Approvals.Verify(Update("5/1"));
			}

			[Fact]
			public void ExistingUnderlineIsIgnored()
			{
				Approvals.Verify(Update("5/1\r\n==============="));
			}

			[Fact]
			public void ItIsReformattedFromAShortDate()
			{
				Approvals.Verify(Update("05/01/2012"));
			}

			[Fact]
			public void ExistingDayTotalIsIgnored()
			{
				Approvals.Verify(Update("05/01/2012\r\n\r\nDay: 0:00"));
			}

			[Fact]
			public void ExistingDayTotalIsIgnoredEvenIfIncorrect()
			{
				Approvals.Verify(Update("05/02/2012\r\n\r\nDay: 1:00"));
			}

			[Fact]
			public void ExistingWeekTotalIsIgnored()
			{
				Approvals.Verify(Update("05/01/2012\r\n\r\nDay: 0:00\r\n\r\nWeek: 0:00"));
			}
		}

		public class WhenATimeIsStarted : StreamFact
		{
			[Fact]
			public void ItIsReformattedFromAMinimalTime()
			{
				Approvals.Verify(Update("5/1\r\n3"));
			}

			[Fact]
			public void ItIsReformattedFromAMinimalTimeWithComma()
			{
				Approvals.Verify(Update("5/1\r\n3,"));
			}
		}

		public class WhenATimeIsEnded : StreamFact
		{
			[Fact]
			public void ItIsReformattedAndDurationsAreCalculated()
			{
				Approvals.Verify(Update("5/1\r\n3, 4:10"));
			}
		}

		protected string Update(string timesheet)
		{
			using (var inputStream = new MemoryStream())
			{
				var writer = new StreamWriter(inputStream);
				writer.Write(timesheet);
				writer.Flush();

				inputStream.Seek(0, SeekOrigin.Begin);

				var outputStream = new UpdateStreamProcessor().Process(inputStream);
				using (var reader = new StreamReader(outputStream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
