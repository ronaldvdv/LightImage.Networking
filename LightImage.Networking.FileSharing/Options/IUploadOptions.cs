using LightImage.Networking.FileSharing.Policies;

namespace LightImage.Networking.FileSharing.Options
{
    public interface IUploadOptions
    {
        RetryPolicyConfig UploadRetryPolicy { get; }
    }
}