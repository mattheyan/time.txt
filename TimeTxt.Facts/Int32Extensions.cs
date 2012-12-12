using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTxt.Facts
{
	public static class Int32Extensions
	{
		public enum AMPM
		{
			AM, PM
		}

		public static TimeSpan OClock(this int hour)
		{
			if (DateTime.Now.Hour >= hour + 12)
			{
				// Could be am or pm, so choose PM since its closer
				return hour.OClock(AMPM.PM);
			}
			else
			{
				// Likely AM, since the current hour is less than the PM hour
				return hour.OClock(AMPM.AM);
			}
		}

		public static TimeSpan OClockAM(this int hour)
		{
			return hour.OClock(AMPM.AM);
		}

		public static TimeSpan OClockPM(this int hour)
		{
			return hour.OClock(AMPM.PM);
		}

		public static TimeSpan OClock(this int hour, AMPM amPm)
		{
			return DateTime.Today.AddHours(hour + (amPm == AMPM.AM ? 0 : 12)).TimeOfDay;
		}

		public static TimeSpan Minutes(this int minutes)
		{
			if (minutes < 0 || minutes >= (60 * 24))
				throw new ArgumentOutOfRangeException();

			return DateTime.Today.AddMinutes(minutes).TimeOfDay;
		}
	}
}
