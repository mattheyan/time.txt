using Xunit;

namespace TimeTxt.Facts
{
	public static class NullableExtensions
	{
		public static void ShouldHaveValue<T>(this T? self)
			where T : struct
		{
			Assert.True(self.HasValue);
		}

		public static void ShouldNotHaveValue<T>(this T? self)
			where T : struct
		{
			Assert.False(self.HasValue);
		}
	}
}
