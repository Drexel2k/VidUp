namespace Drexel.VidUp.Youtube.VideoUpload
{
    public class UploadResult
    {
        public VideoResult VideoResult { get; set; }

        public bool ThumbnailSuccessFull { get; set; }

        public bool PlaylistSuccessFull { get; set; }
    }
}
