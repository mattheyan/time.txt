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
			int hoursToAdd;
			if (hour == 12 && amPm == AMPM.AM)
				hoursToAdd = 0;
			else if (hour == 12 && amPm == AMPM.PM)
				hoursToAdd = 12;
			else
				hoursToAdd = hour + (amPm == AMPM.PM ? 12 : 0);

			return DateTime.Today.AddHours(hoursToAdd).TimeOfDay;
		}

		public static TimeSpan Minutes(this int minutes)
		{
			if (minutes < 0 || minutes >= (60 * 24))
				throw new ArgumentOutOfRangeException();

			return DateTime.Today.AddMinutes(minutes).TimeOfDay;
		}
	}
}
