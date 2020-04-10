using Polly;
using Polly.Retry;

namespace LightImage.Util.Polly
{
    public static class PollyExtensions
    {
        public static AsyncRetryPolicy WaitAndRetryAsync(this PolicyBuilder builder, RetryPolicy policy)
        {
            return builder.WaitAndRetryAsync(policy.MaxAttempts - 1, attempt => policy.GetInterval(attempt));
        }
    }
}