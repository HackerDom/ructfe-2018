using System;

namespace NTPTools
{
    public static class TimeExtensions
    {
        public static double ToUnixTimestamp(this DateTime source)
        {
            return source.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        public static DateTime FromUnixTimestamp(this double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTime FromUnixTimestamp(this ulong unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }
    }
}