namespace Drexel.VidUp.Youtube.Service
{
    public class UploadResult
    {
        public VideoResult VideoResult { get; set; }

        public bool ThumbnailSuccessFull { get; set; }

        public bool PlaylistSuccessFull { get; set; }
    }
}
