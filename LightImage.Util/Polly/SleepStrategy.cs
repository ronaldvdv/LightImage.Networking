using System;

namespace LightImage.Util.Polly
{
    public abstract class SleepStrategy
    {
        public static SleepStrategy Constant = new ConstantSleepStrategy();

        public static SleepStrategy Exponential = new ExponentialSleepStrategy();

        public static SleepStrategy Linear = new LinearSleepStrategy();

        public static SleepStrategy Get(SleepType type)
        {
            switch (type)
            {
                case SleepType.Constant:
                    return Constant;

                case SleepType.Exponential:
                    return Exponential;

                case SleepType.Linear:
                    return Linear;

                default:
                    throw new NotSupportedException($"Unspported sleep type {type}");
            }
        }

        public abstract TimeSpan GetSleep(RetryPolicy config, int interval);

        private class ConstantSleepStrategy : SleepStrategy
        {
            public override TimeSpan GetSleep(RetryPolicy config, int interval)
            {
                return config.Sleep;
            }
        }

        private class ExponentialSleepStrategy : SleepStrategy
        {
            public override TimeSpan GetSleep(RetryPolicy config, int interval)
            {
                var result = config.Sleep.Multiply(Math.Pow(config.Factor, interval));
                return TimeSpanExtensions.Min(config.Bound, result);
            }
        }

        private class LinearSleepStrategy : SleepStrategy
        {
            public override TimeSpan GetSleep(RetryPolicy config, int interval)
            {
                return TimeSpanExtensions.Min(config.Bound, config.Sleep + config.Increase.Multiply(interval));
            }
        }
    }
}