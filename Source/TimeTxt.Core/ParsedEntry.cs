using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTxt.Core
{
	public class ParsedEntry
	{
		public DateTime? Start { get; internal set; }
		public DateTime? End { get; internal set; }
		public string Notes { get; internal set; }

		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			if (Start.HasValue)
			{
				bool startPm;

				if (Start.Value.Hour >= 12)
				{
					startPm = true;
					builder.Append(Start.Value.Hour - 12);
				}
				else
				{
					startPm = false;
					builder.Append(Start.Value.Hour);
				}

				if (Start.Value.Minute > 0)
				{
					builder.Append(":");
					builder.Append(Start.Value.Minute.ToString("00"));
				}

				if (startPm)
					builder.Append("p, ");
				else
					builder.Append("a, ");
			}

			if (End.HasValue)
			{
				bool endPm;

				if (End.Value.Hour >= 12)
				{
					endPm = true;
					builder.Append(End.Value.Hour - 12);
				}
				else
				{
					endPm = false;
					builder.Append(End.Value.Hour);
				}

				if (End.Value.Minute > 0)
				{
					builder.Append(":");
					builder.Append(End.Value.Minute.ToString("00"));
				}

				if (endPm)
					builder.Append("p, ");
				else
					builder.Append("a, ");
			}

			if (Notes != null)
				builder.Append(Notes);

			return builder.ToString();
		}
	}
}
