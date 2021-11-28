namespace Drexel.VidUp.Youtube.VideoUploadService
{
    public enum UploadResult
    {
        FailedWithoutDataSent,
        FailedWithDataSent,
        StoppedWithoutDataSent,
        StoppedWithDataSent,
        Finished
    }
}