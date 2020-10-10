
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeThumbnailService
    {
        private static string thumbnailEndpoint = "https://www.googleapis.com/upload/youtube/v3/thumbnails/set";

        public static async Task<bool> AddThumbnail(Upload upload)
        {
            if (!string.IsNullOrWhiteSpace(upload.VideoId) && !string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                try
                {
                    HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            string.Format("{0}?videoId={1}", YoutubeThumbnailService.thumbnailEndpoint, upload.VideoId), "POST", upload.ThumbnailFilePath);

                    using (FileStream inputStream = new FileStream(upload.ThumbnailFilePath, FileMode.Open))
                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        byte[] buffer = new byte[1024];
                        int bytesRead;
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await dataStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }

                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                    }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        using (e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Thumbnail upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (Exception e)
                {
                    upload.UploadErrorMessage = $"Thumbnail upload failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
