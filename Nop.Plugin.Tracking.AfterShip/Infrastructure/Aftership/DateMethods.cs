using System;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership
{
    public static class DateMethods
    {
        public static string ToIso8601Short(this DateTime date)
        {
            // since we pass it to UniversalTime we can add the +00:00 manually
            return date.ToString ("yyyy'-'MM'-'dd'T'HH':'mm':'sszzz");
        }
    }
}