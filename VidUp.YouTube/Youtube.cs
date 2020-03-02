using Drexel.VidUp.Business;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Drexel.VidUp.YouTube
{
    public class Youtube
    {
        public static async Task Upload(Upload upload, Action<IUploadProgress> videosInsertRequest_ProgressChanged, Action<Video> videosInsertRequest_ResponseReceived)
        {
            upload.UploadStatus = UplStatus.Uploading;
            upload.UploadStart = DateTime.Now;

            await Run(upload, videosInsertRequest_ProgressChanged, videosInsertRequest_ResponseReceived);
        }

        private static async Task Run(Upload upload, Action<IUploadProgress> videosInsertRequest_ProgressChanged, Action<Video> videosInsertRequest_ResponseReceived)
        {
            UserCredential credential;
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = Credentials.ClientId,
                    ClientSecret = Credentials.ClientSecret
                },
                // This OAuth 2.0 access scope allows an application to upload files to the
                // authenticated user's YouTube channel, but doesn't allow other types of access.
                new[] { YouTubeService.Scope.YoutubeUpload },
                "Drexel.Development.VidUp",
                CancellationToken.None
            ); 
          
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Assembly.GetExecutingAssembly().GetName().Name
            });

            var video = new Video();
            video.Snippet = new VideoSnippet();
            video.Snippet.Title = upload.YtTitle;
            video.Snippet.Description = upload.Template != null ? upload.Template.YtDescription : null;

            List<string> tags = new List<string>();
            tags.AddRange(upload.Template != null ? upload.Template.Tags : new List<string>());
            tags.AddRange(upload.AdditonalTags);
            video.Snippet.Tags = tags;

            video.Status = new VideoStatus();

            video.Status.PrivacyStatus = "private";
            if (upload.Template != null)
            {
                 video.Status.PrivacyStatus = upload.Template.YtVisibility.ToString().ToLower(); // "unlisted", "private" or "public"
            }

            if (upload.PublishAt > DateTime.MinValue)
            {
                video.Status.PublishAt = upload.PublishAt;
            }
                  
            var filePath = upload.FilePath;

            using (var fileStream = new FileStream(filePath, FileMode.Open))
            {
                var videosInsertRequest = youtubeService.Videos.Insert(video, "snippet,status", fileStream, "video/*");
                videosInsertRequest.ProgressChanged += videosInsertRequest_ProgressChanged;
                videosInsertRequest.ResponseReceived += videosInsertRequest_ResponseReceived;

                await videosInsertRequest.UploadAsync();
            }
        }
    }
}
