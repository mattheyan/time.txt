using System;
using System.Globalization;

namespace TimeTxt.Core.Extensions
{
	public static class DateTimeExtensions
	{
		public static bool IsOn(this DateTime dateTime, Date date)
		{
			return dateTime.Date.Equals(date.AsDateTime(dateTime.Kind == DateTimeKind.Unspecified ? DateTimeKind.Local : dateTime.Kind));
		}

		public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, DateTimeStyles style, out DateTime result, out string matchingFormat)
		{
			foreach (var format in formats)
			{
				if (DateTime.TryParseExact(s, format, provider, style, out result))
				{
					matchingFormat = format;
					return true;
				}
			}

			result = default(DateTime);
			matchingFormat = null;
			return false;
		}
	}
}
