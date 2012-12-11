using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TimeTxt.Facts
{
	public static class TimeParser
	{
		public static void ShouldMatch(string input)
		{
			Assert.True(TimeTxt.TimeParser.Matches(input));
		}

		public static void ShouldNotMatch(string input)
		{
			Assert.False(TimeTxt.TimeParser.Matches(input));
		}
	}
}
