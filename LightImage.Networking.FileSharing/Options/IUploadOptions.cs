using LightImage.Util.Polly;

namespace LightImage.Networking.FileSharing.Options
{
    public interface IUploadOptions
    {
        RetryPolicy UploadRetryPolicy { get; }
    }
}