using System;
using System.Text;

namespace MMBot
{
    public static class DateTimeExtensions
    {
        public static string GetRelativeTime(this DateTime d)
        {
            // 1.
            // Get time span elapsed since the date.
            TimeSpan s = ((d.Kind == DateTimeKind.Utc) ? DateTime.UtcNow : DateTime.Now).Subtract(d);

            // 2.
            // Get total number of days elapsed.
            int dayDiff = (int)s.TotalDays;

            // 3.
            // Get total number of seconds elapsed.
            int secDiff = (int)s.TotalSeconds;

            // 4.
            // Don't allow out of range values.
            if (dayDiff < 0 || dayDiff >= 31)
            {
                return null;
            }

            // 5.
            // Handle same-day times.
            if (dayDiff == 0)
            {
                // A.
                // Less than one minute ago.
                if (secDiff < 60)
                {
                    return "just now";
                }
                // B.
                // Less than 2 minutes ago.
                if (secDiff < 120)
                {
                    return "1 minute ago";
                }
                // C.
                // Less than one hour ago.
                if (secDiff < 3600)
                {
                    return string.Format("{0} minutes ago",
                        Math.Floor((double)secDiff / 60));
                }
                // D.
                // Less than 2 hours ago.
                if (secDiff < 7200)
                {
                    return "1 hour ago";
                }
                // E.
                // Less than one day ago.
                if (secDiff < 86400)
                {
                    return string.Format("{0} hours ago",
                        Math.Floor((double)secDiff / 3600));
                }
            }
            // 6.
            // Handle previous days.
            if (dayDiff == 1)
            {
                return "yesterday";
            }
            if (dayDiff < 7)
            {
                return string.Format("{0} days ago",
                dayDiff);
            }
            if (dayDiff < 31)
            {
                return string.Format("{0} weeks ago",
                Math.Ceiling((double)dayDiff / 7));
            }
            return null;
        }

        public static long ToEpochTime(this DateTime dt, bool toMilliseconds = false)
        {
            var seconds = (long)(dt - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
            return toMilliseconds ? seconds * 1000 : seconds;
        }

        /// <summary>
        /// Returns a unix Epoch time if given a value, and null otherwise.
        /// </summary>
        public static long? ToEpochTime(this DateTime? dt)
        {
            return
                dt.HasValue ?
                    (long?)ToEpochTime(dt.Value) :
                    null;
        }

        /// <summary>
        /// Converts to Date given an Epoch time
        /// </summary>
        public static DateTime ToDateTime(this long epoch)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(epoch);
        }

        /// <summary>
        /// Returns a humanized string indicating how long ago something happened, eg "3 days ago".
        /// For future dates, returns when this DateTime will occur from DateTime.UtcNow.
        /// </summary>
        public static string ToRelativeTime(this DateTime dt, bool includeTime = true, bool asPlusMinus = false, DateTime? compareTo = null, bool includeSign = true)
        {
            var comp = (compareTo ?? DateTime.UtcNow);
            if (asPlusMinus)
            {
                return dt <= comp ? ToRelativeTimePastSimple(dt, comp, includeSign) : ToRelativeTimeFutureSimple(dt, comp, includeSign);
            }
            return dt <= comp ? ToRelativeTimePast(dt, comp, includeTime) : ToRelativeTimeFuture(dt, comp, includeTime);
        }
        /// <summary>
        /// Returns a humanized string indicating how long ago something happened, eg "3 days ago".
        /// For future dates, returns when this DateTime will occur from DateTime.UtcNow.
        /// If this DateTime is null, returns empty string.
        /// </summary>
        public static string ToRelativeTime(this DateTime? dt, bool includeTime = true)
        {
            if (dt == null) return "";
            return ToRelativeTime(dt.Value, includeTime);
        }

        private static string ToRelativeTimePast(DateTime dt, DateTime utcNow, bool includeTime = true)
        {
            TimeSpan ts = utcNow - dt;
            double delta = ts.TotalSeconds;

            if (delta < 1)
            {
                return "just now";
            }
            if (delta < 60)
            {
                return ts.Seconds == 1 ? "1 sec ago" : ts.Seconds + " secs ago";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "1 min ago" : ts.Minutes + " mins ago";
            }
            if (delta < 86400)  // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours == 1 ? "1 hour ago" : ts.Hours + " hours ago";
            }

            var days = ts.Days;
            if (days == 1)
            {
                return "yesterday";
            }
            if (days <= 2)
            {
                return days + " days ago";
            }
            if (utcNow.Year == dt.Year)
            {
                return dt.ToString(includeTime ? "MMM %d 'at' %H:mmm" : "MMM %d");
            }
            return dt.ToString(includeTime ? @"MMM %d \'yy 'at' %H:mmm" : @"MMM %d \'yy");
        }

        private static string ToRelativeTimeFuture(DateTime dt, DateTime utcNow, bool includeTime = true)
        {
            TimeSpan ts = dt - utcNow;
            double delta = ts.TotalSeconds;

            if (delta < 1)
            {
                return "just now";
            }
            if (delta < 60)
            {
                return ts.Seconds == 1 ? "in 1 second" : "in " + ts.Seconds + " seconds";
            }
            if (delta < 3600) // 60 mins * 60 sec
            {
                return ts.Minutes == 1 ? "in 1 minute" : "in " + ts.Minutes + " minutes";
            }
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
            {
                return ts.Hours == 1 ? "in 1 hour" : "in " + ts.Hours + " hours";
            }

            // use our own rounding so we can round the correct direction for future
            var days = (int)Math.Round(ts.TotalDays, 0);
            if (days == 1)
            {
                return "tomorrow";
            }
            if (days <= 10)
            {
                return "in " + days + " day" + (days > 1 ? "s" : "");
            }
            // if the date is in the future enough to be in a different year, display the year
            if (utcNow.Year == dt.Year)
            {
                return "on " + dt.ToString(includeTime ? "MMM %d 'at' %H:mmm" : "MMM %d");
            }
            return "on " + dt.ToString(includeTime ? @"MMM %d \'yy 'at' %H:mmm" : @"MMM %d \'yy");
        }

        private static string ToRelativeTimePastSimple(DateTime dt, DateTime utcNow, bool includeSign)
        {
            TimeSpan ts = utcNow - dt;
            var sign = includeSign ? "-" : "";
            double delta = ts.TotalSeconds;
            if (delta < 1)
                return "< 1 sec";
            if (delta < 60)
                return sign + ts.Seconds + " sec" + (ts.Seconds == 1 ? "" : "s");
            if (delta < 3600) // 60 mins * 60 sec
                return sign + ts.Minutes + " min" + (ts.Minutes == 1 ? "" : "s");
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
                return sign + ts.Hours + " hour" + (ts.Hours == 1 ? "" : "s");
            return sign + ts.Days + " days";
        }

        private static string ToRelativeTimeFutureSimple(DateTime dt, DateTime utcNow, bool includeSign)
        {
            TimeSpan ts = dt - utcNow;
            double delta = ts.TotalSeconds;
            var sign = includeSign ? "+" : "";

            if (delta < 1)
                return "< 1 sec";
            if (delta < 60)
                return sign + ts.Seconds + " sec" + (ts.Seconds == 1 ? "" : "s");
            if (delta < 3600) // 60 mins * 60 sec
                return sign + ts.Minutes + " min" + (ts.Minutes == 1 ? "" : "s");
            if (delta < 86400) // 24 hrs * 60 mins * 60 sec
                return sign + ts.Hours + " hour" + (ts.Hours == 1 ? "" : "s");
            return sign + ts.Days + " days";
        }

        public static string ToTimeStringMini(this TimeSpan span, int maxElements = 2)
        {
            var sb = new StringBuilder();
            var elems = 0;
            Action<string, int> add = (s, i) =>
            {
                if (elems < maxElements && i > 0)
                {
                    sb.AppendFormat("{0:0}{1} ", i, s);
                    elems++;
                }
            };
            add("d", span.Days);
            add("h", span.Hours);
            add("m", span.Minutes);
            add("s", span.Seconds);
            add("ms", span.Milliseconds);

            if (sb.Length == 0) sb.Append("0");

            return sb.ToString().Trim();
        }
    }
}