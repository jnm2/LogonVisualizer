using System;
using System.Globalization;

namespace LogonVisualizer
{
    internal static class Extensions
    {
        public static DateTime StartOfWeek(this DateTime date, DateTimeFormatInfo formatInfo)
        {
            if (formatInfo is null) throw new ArgumentNullException(nameof(formatInfo));
            date = date.Date;

            return date.AddDays(-((7 + (date.DayOfWeek - formatInfo.FirstDayOfWeek)) % 7));
        }
    }
}
