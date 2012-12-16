using ApprovalTests;
using ApprovalTests.Reporters;
using System.IO;
using TimeTxt.Core;
using Xunit;

namespace TimeTxt.ApprovalTests
{
	//[UseReporter(typeof(WinMergeReporter))]
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
				Approvals.Verify(Update(
@"5/1
==============="
				));
			}

			[Fact]
			public void ItIsReformattedFromAShortDate()
			{
				Approvals.Verify(Update("05/01/2012"));
			}

			[Fact]
			public void ExistingDayTotalIsIgnored()
			{
				Approvals.Verify(Update(
@"05/01/2012

Day: 0:00"
				));
			}

			[Fact]
			public void ExistingDayTotalIsIgnoredEvenIfIncorrect()
			{
				Approvals.Verify(Update(
@"05/02/2012

Day: 1:00"
				));
			}

			[Fact]
			public void ExistingWeekTotalIsIgnored()
			{
				Approvals.Verify(Update(
@"05/01/2012

Day: 0:00

Week: 0:00"
				));
			}
		}

		public class WhenATimeIsStarted : StreamFact
		{
			[Fact]
			public void ItIsReformattedFromAMinimalTime()
			{
				Approvals.Verify(Update(
@"5/1
3"
				));
			}

			[Fact]
			public void ItIsReformattedFromAMinimalTimeWithComma()
			{
				Approvals.Verify(Update(
@"5/1
3,"
				));
			}
		}

		public class WhenATimeIsEnded : StreamFact
		{
			[Fact]
			public void ItIsReformattedAndDurationsAreCalculated()
			{
				Approvals.Verify(Update(
@"5/1
3, 4:10"
				));
			}

			[Fact]
			public void StartCanIncludeAM()
			{
				Approvals.Verify(Update(
@"5/1
9a, 12"
				));
			}

			[Fact]
			public void StartCanIncludePM()
			{
				Approvals.Verify(Update(
@"5/1
1p, 3"
				));
			}

			[Fact]
			public void EndCanIncludeAM()
			{
				Approvals.Verify(Update(
@"5/1
9, 11a"
				));
			}

			[Fact]
			public void EndCanIncludePM()
			{
				Approvals.Verify(Update(
@"5/1
1, 3p"
				));
			}

			[Fact]
			public void ItCanSpanAcrossNoonWithoutNotes()
			{
				Approvals.Verify(Update(
@"5/1
9, 9"
				));
			}

			[Fact]
			public void ItCanSpanAcrossNoon()
			{
				Approvals.Verify(Update(
@"5/1
9, 9, blah"
				));
			}

			[Fact]
			public void ExistingDurationIsIgnoredEvenIfIncorrect()
			{
				Approvals.Verify(Update(
@"5/1
(0:05) 3, 4:10"
				));
			}
		}

		public class WhenNotesAreEntered : StreamFact
		{
			[Fact]
			public void TheyArePreservedInTheOutput()
			{
				Approvals.Verify(Update(
@"5/1
3, 4:10, blah blah blah"
				));
			}

			[Fact]
			public void TheyArePreservedEvenIfTimeIsNotEnded()
			{
				Approvals.Verify(Update(
@"5/1
3, blah blah blah"
				));
			}
		}

		public class WhenMultipleDaysAreEntered : StreamFact
		{
			[Fact]
			public void TheCorrectSpacingIsUsed()
			{
				Approvals.Verify(Update(
	@"5/1
3, 4, blah blah blah

Day: 1:00

5/3
4, 5, blah

Day: 1:00

Week: 1:00
"
				));
			}
		}

		public class WhenAFullWeekIsEntered : StreamFact
		{
			[Fact]
			public void TheSumCarriesOver24Hours()
			{
				Approvals.Verify(Update(
	@"5/1
9,5

5/2
9,5

5/3
9,5

5/4
9,5

5/5
9,5
"
				));
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

				var outputStream = new MemoryStream();
				new UpdateStreamProcessor(7).Update(inputStream, outputStream);
				outputStream.Seek(0, SeekOrigin.Begin);
				using (var reader = new StreamReader(outputStream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
