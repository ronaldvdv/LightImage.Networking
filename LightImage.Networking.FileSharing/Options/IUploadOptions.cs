using LightImage.Polly;

namespace LightImage.Networking.FileSharing.Options
{
    public interface IUploadOptions
    {
        RetryPolicyConfig UploadRetryPolicy { get; }
    }
}