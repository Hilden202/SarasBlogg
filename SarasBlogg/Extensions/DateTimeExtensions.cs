using System;

namespace SarasBlogg.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly TimeZoneInfo SwedishZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

        /// <summary>
        /// Konverterar ett UTC-datum till svensk tid (tar hänsyn till sommar/vintertid).
        /// </summary>
        public static DateTime ToSwedishTime(this DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, SwedishZone);
        }
    }
}
