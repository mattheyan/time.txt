using System;

namespace TimeTxt.Core
{
	public struct Date
	{
		private DateTime? utcDate;

		private DateTime? localDate;

		public Date(int year, int month, int day)
			: this()
		{
			Year = year;
			Month = month;
			Day = day;
		}

		public Date(DateTime dateTime)
			: this(dateTime.Year, dateTime.Month, dateTime.Day)
		{
		}

		public int Year { get; set; }

		public int Month { get; set; }

		public int Day { get; set; }

		public DateTime UtcDate
		{
			get
			{
				if (utcDate == null)
					utcDate = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Utc);
				return utcDate.Value;
			}
		}

		public DateTime LocalDate
		{
			get
			{
				if (localDate == null)
					localDate = new DateTime(Year, Month, Day, 0, 0, 0, DateTimeKind.Local);
				return localDate.Value;
			}
		}

		public static Date Today
		{
			get
			{
				return new Date(DateTime.Today);
			}
		}

		public override string ToString()
		{
			return string.Format("{0}/{1}/{2}", Month.ToString("00"), Day.ToString("00"), Year.ToString("0000"));
		}

		public string ToString(string format)
		{
			return UtcDate.ToString(format);
		}

		public DateTime AsDateTime(DateTimeKind kind)
		{
			return new DateTime(Year, Month, Day, 0, 0, 0, kind);
		}
	}
}
