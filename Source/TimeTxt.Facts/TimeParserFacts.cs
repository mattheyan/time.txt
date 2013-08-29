using System;
using Chronos;
using Should;
using Should.Fluent;
using Xunit;

namespace TimeTxt.Facts
{
	public class TimeParserFacts
	{
		protected static TimeSpan Midnight { get { return DateTime.Today.TimeOfDay; } }

		protected static TimeSpan Noon { get { return DateTime.Today.AddHours(12).TimeOfDay; } }

		public class ASingleDigit
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3");
			}

			[Fact]
			public void IsASimpleInferredStartTime()
			{
				var parsed = Core.TimeParser.Parse("3", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, ");
				parsed.ToString(true).Should().Equal("3a, ");
			}

			[Fact]
			public void IsASimpleInferredStartTimeInTheAfternoon()
			{
				var parsed = Core.TimeParser.Parse("3", Date.Today, Noon);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockPM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3p, ");
				parsed.ToString(true).Should().Equal("3p, ");
			}

			[Fact]
			public void Of12IsInferredAsNoon()
			{
				var parsed = Core.TimeParser.Parse("12", Date.Today, Noon);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(12.OClockPM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("12p, ");
				parsed.ToString(true).Should().Equal("12p, ");
			}

		}

		public class ASingleDigitWithAM
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3a");
			}

			[Fact]
			public void IsASimpleStartTime()
			{
				var parsed = Core.TimeParser.Parse("3a", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, ");
				parsed.ToString(true).Should().Equal("3a, ");
			}
		}

		public class AnAlphabetCharacter
		{
			[Fact]
			public void IsNotMatched()
			{
				TimeParser.ShouldNotMatch("x");
			}
		}

		public class ASpecialCharacter
		{
			[Fact]
			public void IsNotMatched()
			{
				TimeParser.ShouldNotMatch("!");
			}
		}

		public class ASingleDigitWithTrailingComma
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3,");
			}

			[Fact]
			public void IsASimpleInferredStartTime()
			{
				var parsed = Core.TimeParser.Parse("3,", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, ");
				parsed.ToString(true).Should().Equal("3a, ");
			}

			[Fact]
			public void IsASimpleInferredStartTimeInTheAfternoon()
			{
				var parsed = Core.TimeParser.Parse("3,", Date.Today, Noon);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockPM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3p, ");
				parsed.ToString(true).Should().Equal("3p, ");
			}
		}

		public class TwoDigitsSeparatedByColon
		{
			[Fact]
			public void IsNotMatched()
			{
				TimeParser.ShouldNotMatch("3:2");
			}

			[Fact]
			public void IsAStartTime()
			{
				var parsed = Core.TimeParser.Parse("3:05", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM().Add(5.Minutes()));
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3:05a, ");
				parsed.ToString(true).Should().Equal("3:05a, ");
			}

			[Fact]
			public void IsAStartTimeInTheAfternoon()
			{
				var parsed = Core.TimeParser.Parse("3:10", Date.Today, Noon);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockPM().Add(10.Minutes()));
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3:10p, ");
				parsed.ToString(true).Should().Equal("3:10p, ");
			}
		}

		public class DigitsThenColon
		{
			[Fact]
			public void IsNotMatched()
			{
				TimeParser.ShouldNotMatch("3:");
			}
		}

		public class DigitCommaDigitWithoutWhitespace
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3,4");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3,4", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockAM());
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, 4a, ");
				parsed.ToString(true).Should().Equal("(1:00) 3a, 4a, ");
			}

			[Fact]
			public void SpansAcrossNoonIfEndIsGreaterThanEqualStart()
			{
				var parsed = Core.TimeParser.Parse("3,3", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(3.OClockPM());
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, 3p, ");
				parsed.ToString(true).Should().Equal("(12:00) 3a, 3p, ");
			}
		}

		public class DigitCommaDigit
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3, 4");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3, 4", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockAM());
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, 4a, ");
				parsed.ToString(true).Should().Equal("(1:00) 3a, 4a, ");
			}
		}

		public class DigitCommaDigitComma
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3, 4,");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3, 4,", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockAM());
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, 4a, ");
				parsed.ToString(true).Should().Equal("(1:00) 3a, 4a, ");
			}
		}

		public class DigitCommaDigitCommaWhitespace
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3, 4,  ");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3, 4,  ", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockAM());
				parsed.Notes.Should().Be.Null();
				parsed.ToString().Should().Equal("3a, 4a, ");
				parsed.ToString(true).Should().Equal("(1:00) 3a, 4a, ");
			}
		}

		public class DigitCommaDigitCommaNotes
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3, 4, blah, blah, blah");
				TimeParser.ShouldMatch("(2:24) 1:37, 4:01, f1 setup, perf analysis, discuss various bugs with Brandon");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3, 4, blah, blah, blah", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockAM());
				parsed.Notes.Should().Equal("blah, blah, blah");
				parsed.ToString().Should().Equal("3a, 4a, blah, blah, blah");
				parsed.ToString(true).Should().Equal("(1:00) 3a, 4a, blah, blah, blah");
			}
		}

		public class TotalDigitCommaDigitCommaNotes
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("(2:24) 1:37, 4:01, computer setup, perf analysis, discuss various bugs");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("(2:24) 1:37, 4:01, computer setup, perf analysis, discuss various bugs", Date.Today, Noon);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(1.OClockPM().Add(37.Minutes()));
				parsed.End.ShouldHaveValue();
				parsed.End.Value.Date.Should().Equal(DateTime.Today);
				parsed.End.Value.TimeOfDay.Should().Equal(4.OClockPM().Add(1.Minutes()));
				parsed.Notes.Should().Equal("computer setup, perf analysis, discuss various bugs");
				parsed.ToString().Should().Equal("1:37p, 4:01p, computer setup, perf analysis, discuss various bugs");
				parsed.ToString(true).Should().Equal("(2:24) 1:37p, 4:01p, computer setup, perf analysis, discuss various bugs");
			}
		}

		public class DigitCommaNotes
		{
			[Fact]
			public void IsMatched()
			{
				TimeParser.ShouldMatch("3, blah, blah, blah");
			}

			[Fact]
			public void IsASimpleInferredStartAndEndTime()
			{
				var parsed = Core.TimeParser.Parse("3, blah, blah, blah", Date.Today, Midnight);
				parsed.Start.Date.Should().Equal(DateTime.Today);
				parsed.Start.TimeOfDay.Should().Equal(3.OClockAM());
				parsed.End.ShouldNotHaveValue();
				parsed.Notes.Should().Equal("blah, blah, blah");
				parsed.ToString().Should().Equal("3a, blah, blah, blah");
				parsed.ToString(true).Should().Equal("3a, blah, blah, blah");
			}
		}
	}
}
