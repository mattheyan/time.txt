using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace TimeTxt.Facts
{
	public static class NullableExtensions
	{
		public static void ShouldHaveValue<T>(this Nullable<T> self)
			where T : struct
		{
			Assert.True(self.HasValue);
		}

		public static void ShouldNotHaveValue<T>(this Nullable<T> self)
			where T : struct
		{
			Assert.False(self.HasValue);
		}
	}
}
