using ApprovalTests;
using ApprovalTests.Reporters;
using System;
using System.IO;
using System.Linq;
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
			public void ItIsNotPreserved()
			{
				Approvals.Verify(Update("\r\n  \r"));
			}
		}

		public class WhenADateIsStarted : StreamFact
		{
			[Fact]
			public void ItIsReformattedFromAMinimalDate()
			{
				Approvals.Verify(Update(@"

					5/1

				"));
			}

			[Fact]
			public void ExistingUnderlineIsIgnored()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					===============

				"));
			}

			[Fact]
			public void ItIsReformattedFromAShortDate()
			{
				Approvals.Verify(Update(@"

					05/01/2012

				"));
			}

			[Fact]
			public void ExistingDayTotalIsIgnored()
			{
				Approvals.Verify(Update(@"
					05/01/2012

					Day: 0:00
				"));
			}

			[Fact]
			public void ExistingDayTotalIsIgnoredEvenIfIncorrect()
			{
				Approvals.Verify(Update(@"

					05/02/2012

					Day: 1:00

				"));
			}

			[Fact]
			public void ExistingWeekTotalIsIgnored()
			{
				Approvals.Verify(Update(@"

					05/01/2012

					Day: 0:00

					Week: 0:00

				"));
			}
		}

		public class WhenADateIsEnded : StreamFact
		{
			[Fact]
			public void TheDayAndWeekTotalsAreUpdated()
			{
				Approvals.Verify(Update(@"

					08/29/2021

					9a, 5p, do stuff

					Day: 0:00

					Week: 0:00

				"));
			}

			[Fact]
			public void TheDayTotalDoesNotIncludeGaps()
			{
				Approvals.Verify(Update(@"

					08/29/2021

					2:19p, 2:25p, thing 1
					2:25p, 2:29p, thing 2
					2:40p, 2:43p, thing 3

					Day: 0:00

					Week: 0:00

				"));
			}
		}

		public class WhenATimeIsStarted : StreamFact
		{
			[Fact]
			public void ItIsReformattedFromAMinimalTime()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3

				"));
			}

			[Fact]
			public void ItIsReformattedFromAMinimalTimeWithComma()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3,

				"));
			}

			[Fact]
			public void ItIsMadePmIfBeforeTheLastStartTime()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					9:13a, 9:30a, do stuff in the morning
					9:05, 10, do stuff some other time

				"));
			}

			[Fact]
			public void AnErrorOccursIfItIsBeforeTheLastStartTimeAndCantBePm()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					3:10p, 4p, do stuff 1
					3, 5, do stuff 2

				"));
			}

			[Fact]
			public void AnErrorOccursIfItIsBeforeTheLastStartTime()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					9:13a, 9:30a, do stuff 1
					9:05a, 5:52, do stuff 2

				"));
			}
		}

		public class WhenATimeIsEnded : StreamFact
		{
			[Fact]
			public void ItIsReformattedAndDurationsAreCalculated()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3, 4:10

				"));
			}

			[Fact]
			public void StartCanIncludeAm()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					9a, 12

				"));
			}

			[Fact]
			public void StartCanIncludePm()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					1p, 3

				"));
			}

			[Fact]
			public void EndCanIncludeAm()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					9, 11a

				"));
			}

			[Fact]
			public void EndCanIncludePm()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					1, 3p

				"));
			}

			[Fact]
			public void ItCanSpanAcrossNoonWithoutNotes()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					9, 9

				"));
			}

			[Fact]
			public void ItCanSpanAcrossNoon()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					9, 9, blah

				"));
			}

			[Fact]
			public void ExistingDurationIsIgnoredEvenIfIncorrect()
			{
				Approvals.Verify(Update(@"
					#! ignoreExistingDurations=True

					5/1/2012
					(0:05) 3, 4:10

				"));
			}
		}

		public class WhenLineIsImproperlyFormatted : StreamFact
		{
			[Fact]
			public void AnErrorOccurs()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					9, 5, work all day
					This will not match any expected line format

					Day: 0:00

				"));
			}
		}

		public class WhenNotesAreEntered : StreamFact
		{
			[Fact]
			public void TheyArePreservedInTheOutput()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3, 4:10, blah blah blah

				"));
			}

			[Fact]
			public void TheyArePreservedEvenIfTimeIsNotEnded()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3, blah blah blah

				"));
			}
		}

		public class WhenMultipleDaysAreEntered : StreamFact
		{
			[Fact]
			public void TheCorrectSpacingIsUsed()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					3, 4, blah blah blah

					Day: 1:00

					5/3/2012
					4, 5, blah

					Day: 1:00

					Week: 1:00

				"));
			}
		}

		public class WhenAFullWeekIsEntered : StreamFact
		{
			[Fact]
			public void TheSumCarriesOver24Hours()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					9,5

					5/2/2012
					9,5

					5/3/2012
					9,5

					5/4/2012
					9,5

					5/5/2012
					9,5

				"));
			}
		}

		public class WhenTheLineStartsWithTheCommentCharacter : StreamFact
		{
			[Fact]
			public void ItIsIgnored()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					#8, 9, ...
					9,10, do stuff

				"));
			}
		}

		public class WhenTheTotalIsOverOneHalfHour : StreamFact
		{
			[Fact]
			public void ItIsProperlyFormatted()
			{
				Approvals.Verify(Update(@"

					5/1/2012
					8:10, 9:42, do stuff

				"));
			}
		}

		public class WhenTimesAreNested : StreamFact
		{
			[Fact]
			public void TheOuterTimeDurationIsReduced()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					8a, 9a, catch-all
					8:15a, 8:30a, do stuff

				"));
			}

			[Fact]
			public void AnErrorOccursIfTheyOverlap()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					9:13a, 5:56, catch-all
					9:05a, 5:52, do stuff

				"));
			}

			[Fact]
			public void MultipleTimesCanBeNested()
			{
				Approvals.Verify(Update(@"

					8/29/2021
					7:46a,  8:02a,  line 1
					8:49a,  8:50a,  line 2
					9:05a,  5:56,   line 3
					9:05a,  9:13a,  line 4
					9:13a,  5:52,   line 5
					9:13a,  9:21a,  line 6
					9:15a,  9:18a,  line 7
					9:21a,  10a,    line 8
					9:33a,  9:37a,  line 9
					9:39a,  9:40a,  line 10
					9:47a,  9:52a,  line 11
					10a,    10:05a, line 12
					10:03a, 10:04a, line 13
					10:05a, 10:07a, line 14
					10:07a, 11:11a, line 15
					10:08a, 10:11a, line 16
					11:11a, 11:26a, line 17
					11:26a, 11:32a, line 18
					11:32a, 11:37a, line 19
					11:37a, 11:42a, line 20
					11:42a, 11:45a, line 21
					#-- (0:15)  11:50a, 12:05p, EXCLUDED
					#-- (0:20)  12:05p, 12:25p, EXCLUDED
					12:26p, 12:46p, line 22
					12:46p, 1p,     line 23
					1p,     1:30p,  line 24
					1:28p,  1:29p,  line 25
					1:29p,  1:30p,  line 26
					1:30p,  2:23p,  line 27
					1:47p,  1:48p,  line 28
					1:49p,  1:50p,  line 29
					1:50p,  1:51p,  line 30
					2:23p,  2:24p,  line 31
					#-- (0:10)  2:28p,  2:38p,  EXCLUDED
					2:38p,  5:52p,  line 32
					5:52p,  5:55p,  line 33

				"));
			}
		}

		public class DebugScenarios : StreamFact
		{
			private string PopDirectories(string path, params string[] dirs)
			{
				if (dirs.Length == 0)
					return path;

				var leafPath = Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, dirs.Select(d => d.ToLower()));
				if (path.ToLower().EndsWith(leafPath))
					return path.Substring(0, path.Length - leafPath.Length);

				return path;
			}

			private string GetProjectPath()
			{
				var dir = Path.GetDirectoryName(typeof(StreamFact).Assembly.Location);
				dir = PopDirectories(dir, "netcoreapp3.1");
				dir = PopDirectories(dir, "bin", "Debug");
				dir = PopDirectories(dir, "Debug");
				return dir;
			}

			//[Fact]
			public void TimeTxtFileIsUpdated()
			{
				var timeFilePath = Path.Combine(GetProjectPath(), "time.txt");
				Assert.True(File.Exists(timeFilePath), $"File '{timeFilePath}' does not exist.");
				var timeFileContent = File.ReadAllText(timeFilePath);
				Approvals.Verify(Update(timeFileContent, earliestStart: null));
			}
		}

		protected string Update(string timesheet, int? earliestStart = 7)
		{
			using (var inputStream = new MemoryStream())
			{
				var writer = new StreamWriter(inputStream);
				writer.Write(timesheet);
				writer.Flush();

				inputStream.Seek(0, SeekOrigin.Begin);

				var outputStream = new MemoryStream();

				var processor = earliestStart.HasValue ? new UpdateStreamProcessor(earliestStart: earliestStart.Value) : new UpdateStreamProcessor();

				processor.Update(inputStream, outputStream, true, out var currentLine);

				outputStream.Seek(0, SeekOrigin.Begin);
				using (var reader = new StreamReader(outputStream))
				{
					return reader.ReadToEnd();
				}
			}
		}
	}
}
