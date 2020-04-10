using LightImage.Util.Polly;

namespace LightImage.FileSharing.Options
{
    public interface IUploadOptions
    {
        RetryPolicy UploadRetryPolicy { get; }
    }
}