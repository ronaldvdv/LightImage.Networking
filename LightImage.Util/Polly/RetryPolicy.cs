using System;

namespace LightImage.Util.Polly
{
    public class RetryPolicy
    {
        private SleepStrategy _strategy = SleepStrategy.Constant;
        private SleepType _type = SleepType.Constant;
        public TimeSpan Bound { get; set; } = TimeSpan.MaxValue;
        public double Factor { get; set; } = 1.0;
        public TimeSpan Increase { get; set; } = TimeSpan.Zero;
        public int MaxAttempts { get; set; } = 1;
        public TimeSpan Sleep { get; set; } = TimeSpan.Zero;

        public SleepType Type {
            get => _type;
            set {
                if (_type == value)
                    return;
                _type = value;
                _strategy = SleepStrategy.Get(Type);
            }
        }

        public static RetryPolicy Constant(int maxAttempts, TimeSpan sleep)
        {
            return new RetryPolicy {
                Type = SleepType.Constant,
                MaxAttempts = maxAttempts,
                Sleep = sleep
            };
        }

        public static RetryPolicy Exponential(int maxAttempts, TimeSpan initial, double factor, TimeSpan bound)
        {
            return new RetryPolicy {
                Type = SleepType.Exponential,
                MaxAttempts = maxAttempts,
                Sleep = initial,
                Factor = factor,
                Bound = bound
            };
        }

        public static RetryPolicy Immediate(int maxAttempts = 1) => Constant(maxAttempts, TimeSpan.Zero);

        public TimeSpan GetInterval(int attempt)
        {
            if (MaxAttempts >= 1 && attempt >= MaxAttempts)
                return TimeSpan.MaxValue;
            return _strategy.GetSleep(this, attempt);
        }
    }
}