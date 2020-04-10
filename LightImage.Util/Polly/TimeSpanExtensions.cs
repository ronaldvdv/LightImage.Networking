using System;

namespace LightImage.Util.Polly
{
    public static class TimeSpanExtensions
    {
        public static TimeSpan Min(TimeSpan a, TimeSpan b)
        {
            return a < b ? a : b;
        }

        public static TimeSpan Multiply(this TimeSpan ts, double factor) => TimeSpan.FromTicks((long)(ts.Ticks * factor));
    }
}