using LightImage.Util.Polly;
using System;

namespace LightImage.Networking.FileSharing.Policies
{
    /// <summary>
    /// Configuration for a <see cref="RetryPolicy"/> which can be serialized/deserialized easily.
    /// </summary>
    public class RetryPolicyConfig
    {
        private SleepStrategy _strategy = SleepStrategy.Constant;
        private SleepType _type = SleepType.Constant;
        public TimeSpan Bound { get; set; } = TimeSpan.MaxValue;
        public double Factor { get; set; } = 1.0;
        public TimeSpan Increase { get; set; } = TimeSpan.Zero;
        public int MaxAttempts { get; set; } = 1;
        public TimeSpan Sleep { get; set; } = TimeSpan.Zero;

        public SleepType Type
        {
            get => _type;
            set
            {
                if (_type == value)
                    return;
                _type = value;
                _strategy = SleepStrategy.Get(Type);
            }
        }

        public static RetryPolicyConfig Constant(int maxAttempts, TimeSpan sleep)
        {
            return new RetryPolicyConfig
            {
                Type = SleepType.Constant,
                MaxAttempts = maxAttempts,
                Sleep = sleep
            };
        }

        public static RetryPolicyConfig Exponential(int maxAttempts, TimeSpan initial, double factor, TimeSpan bound)
        {
            return new RetryPolicyConfig
            {
                Type = SleepType.Exponential,
                MaxAttempts = maxAttempts,
                Sleep = initial,
                Factor = factor,
                Bound = bound
            };
        }

        public static RetryPolicyConfig Immediate(int maxAttempts = 1) => Constant(maxAttempts, TimeSpan.Zero);

        public TimeSpan GetInterval(int attempt)
        {
            if (MaxAttempts >= 1 && attempt >= MaxAttempts)
                return TimeSpan.MaxValue;
            return _strategy.GetSleep(this, attempt);
        }
    }
}