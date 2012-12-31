using Xunit;

namespace TimeTxt.Facts
{
	public static class TimeParser
	{
		public static void ShouldMatch(string input)
		{
			Assert.True(Core.TimeParser.Matches(input));
		}

		public static void ShouldNotMatch(string input)
		{
			Assert.False(Core.TimeParser.Matches(input));
		}
	}
}
