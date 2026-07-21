using System;

namespace SporticoApp.Shared.Helpers
{
    /// <summary>
    /// Business-day boundaries for admin dashboards, computed in Asia/Ho_Chi_Minh (UTC+7, no DST)
    /// and converted back to UTC for querying UTC-stored <c>timestamp with time zone</c> columns.
    /// The week starts on Monday (matches Postgres' own <c>date_trunc('week', ...)</c> default, used
    /// server-side for chart buckets so rolling "today/this week" stats and chart totals reconcile).
    /// </summary>
    public static class VietnamTimeZone
    {
        public const string IanaId = "Asia/Ho_Chi_Minh";

        private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(IanaId);

        /// <summary>The current instant, expressed in VN local time.</summary>
        public static DateTime NowVn() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone);

        /// <summary>Start of the VN-local calendar day containing <paramref name="utcInstant"/>, returned in UTC.</summary>
        public static DateTime StartOfDayUtc(DateTime utcInstant) => ConvertBack(ToVnLocal(utcInstant).Date);

        /// <summary>Start of the VN-local Monday-based week containing <paramref name="utcInstant"/>, returned in UTC.</summary>
        public static DateTime StartOfWeekUtc(DateTime utcInstant)
        {
            var vnDate = ToVnLocal(utcInstant).Date;
            var diff = (int)vnDate.DayOfWeek == 0 ? 6 : (int)vnDate.DayOfWeek - 1; // Sunday=0 -> 6
            return ConvertBack(vnDate.AddDays(-diff));
        }

        /// <summary>Start of the VN-local calendar month containing <paramref name="utcInstant"/>, returned in UTC.</summary>
        public static DateTime StartOfMonthUtc(DateTime utcInstant)
        {
            var vn = ToVnLocal(utcInstant);
            return ConvertBack(new DateTime(vn.Year, vn.Month, 1, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <summary>Start of the VN-local calendar year containing <paramref name="utcInstant"/>, returned in UTC.</summary>
        public static DateTime StartOfYearUtc(DateTime utcInstant)
        {
            var vn = ToVnLocal(utcInstant);
            return ConvertBack(new DateTime(vn.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));
        }

        private static DateTime ToVnLocal(DateTime utcInstant)
        {
            var utc = utcInstant.Kind == DateTimeKind.Utc
                ? utcInstant
                : DateTime.SpecifyKind(utcInstant, DateTimeKind.Utc);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, Zone);
        }

        private static DateTime ConvertBack(DateTime vnLocalUnspecified)
            => TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(vnLocalUnspecified, DateTimeKind.Unspecified), Zone);
    }
}
