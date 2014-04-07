using System;

namespace TimeTxt.Core.Extensions
{
	public static class DateTimeExtensions
	{
		public static bool IsOn(this DateTime dateTime, Date date)
		{
			return dateTime.Date.Equals(date.AsDateTime(dateTime.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : dateTime.Kind));
		}
	}
}
