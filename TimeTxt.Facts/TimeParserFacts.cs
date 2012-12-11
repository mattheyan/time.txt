using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TimeTxt.Facts
{
	public class TimeParserFacts
	{
		public class ASingleDigit
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3"));
			}

			//[Fact]
			//public void IsASimpleInferredStartTime()
			//{
			//}
		}

		public class AnAlphabetCharacter
		{
			[Fact]
			public void IsNotMatched()
			{
				Assert.False(TimeParser.Matches("x"));
			}
		}

		public class ASpecialCharacter
		{
			[Fact]
			public void IsNotMatched()
			{
				Assert.False(TimeParser.Matches("!"));
			}
		}

		public class ASingleDigitWithTrailingComma
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3,"));
			}
		}

		public class TwoDigitsSeparatedByColon
		{
			[Fact]
			public void IsNotMatched()
			{
				Assert.False(TimeParser.Matches("3:2"));
			}
		}

		public class DigitCommaDigitWithoutWhitespace
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3,4"));
			}
		}

		public class DigitCommaDigit
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3, 4"));
			}
		}

		public class DigitCommaDigitComma
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3, 4,"));
			}
		}

		public class DigitCommaDigitJunk
		{
			[Fact]
			public void IsMatched()
			{
				Assert.False(TimeParser.Matches("3, 4 baz"));
			}
		}

		public class DigitCommaDigitCommaDescription
		{
			[Fact]
			public void IsMatched()
			{
				Assert.True(TimeParser.Matches("3, 4, blah blah blah"));
			}
		}

	}
}
