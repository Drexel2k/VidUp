﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
using Drexel.VidUp.Youtube.VideoUploadService.Data;
using HeyRed.Mime;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUploadService
{
    public class YoutubeVideoUploadService
    {
        private static string videoUploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos";

        //to set max upload speed while running
        private static ThrottledBufferedStream stream = null;
        private static long maxUploadInBytesPerSecond = 0;

        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                Tracer.Write($"YoutubeVideoUploadService.MaxUploadInBytesPerSecond: Sart, setting MaxUploadInBytesPerSecond: {value}.");
                YoutubeVideoUploadService.maxUploadInBytesPerSecond = value;
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    Tracer.Write($"YoutubeVideoUploadService.MaxUploadInBytesPerSecond:Setting throttle in stream.");
                    stream.MaximumBytesPerSecond = value;
                }
                else
                {
                    Tracer.Write($"YoutubeVideoUploadService.MaxUploadInBytesPerSecond: No upload stream.");
                }

                Tracer.Write($"YoutubeVideoUploadService.MaxUploadInBytesPerSecond: End.");
            }
        }

        public static long CurrentSpeedInBytesPerSecond
        {
            get
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    return stream.CurrentSpeedInBytesPerSecond;
                }

                return 0;
            }
        }

        public static long? CurrentPosition
        {
            get
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    return stream.Position;
                }

                return null;
            }
        }

        public static async Task<UploadResult> UploadAsync(Upload upload, Action<Upload> resumableSessionUriSet, CancellationToken cancellationToken, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {YoutubeVideoUploadService.maxUploadInBytesPerSecond}.");

            upload.ClearUploadErrors();

            if (!File.Exists(upload.FilePath))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, file doesn't exist.");

                upload.AddStatusInformation(StatusInformationCreator.Create("ERR0020", "File does not exist."));
                resetEvent.WaitOne();
                upload.UploadStatus = UplStatus.Failed;
                return UploadResult.FailedWithoutDataSent;
            }

            try
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initialize upload.");
                if (!await YoutubeVideoUploadService.initializeUploadAsync(upload, resumableSessionUriSet, resetEvent).ConfigureAwait(false))
                {
                    return UploadResult.FailedWithoutDataSent;
                }

                long initialUploadBytesSent = upload.BytesSent;
                long lastResumePositionBeforeError = upload.BytesSent;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initial resume position: {upload.BytesSent}.");

                using (YoutubeVideoUploadService.stream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond))
                {
                    long fileLength = upload.FileLength;
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    bool error = false;

                    Tracer.Write($"YoutubeVideoUploadService.Upload: fileLength: {fileLength}.");

                    do
                    {
                        if (error)
                        {
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload not successful after 3 tries for resume position {lastResumePositionBeforeError}.");

                                upload.AddStatusInformation(StatusInformationCreator.Create("ERR0021", $"Upload not successful after 3 tries for resume position {lastResumePositionBeforeError}."));
                                resetEvent.WaitOne();
                                upload.UploadStatus = UplStatus.Failed;

                                YoutubeVideoUploadService.stream = null;
                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            error = false;

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Getting range due to upload retry.");
                            if(!await YoutubeVideoUploadService.getResumePositionAsync(upload, resetEvent).ConfigureAwait(false))
                            {
                                YoutubeVideoUploadService.stream = null;
                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            //HttpClient disposed inputStream...
                            YoutubeVideoUploadService.stream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond); ;

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload retry resume postion: {upload.BytesSent}.");

                            if (upload.BytesSent != lastResumePositionBeforeError)
                            {
                                uploadTry = 1;
                                lastResumePositionBeforeError = upload.BytesSent;
                            }

                            upload.AddStatusInformation(StatusInformationCreator.Create("INF0017", $"Resumed upload on position {upload.BytesSent.ToString("N0")} try {uploadTry}."));
                        }

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload try: resume position {upload.BytesSent.ToString("N0")} Try {uploadTry}.");
                        Tracer.Write($"YoutubeVideoUploadService.Upload: Creating content.");

                        using (StreamContent streamContent = HttpHelper.GetStreamContentResumableUpload(YoutubeVideoUploadService.stream, YoutubeVideoUploadService.stream.Length, upload.BytesSent, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(upload.YoutubeAccount, HttpMethod.Put, upload.ResumableSessionUri).ConfigureAwait(false))
                                {
                                    
                                    requestMessage.Content = streamContent;
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: Start uploading.");
                                    using (HttpResponseMessage responseMessage = await HttpHelper.UploadClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false))
                                    using (responseMessage.Content)
                                    {
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Uploading finished.");
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer.");
                                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer finished.");

                                        if (!responseMessage.IsSuccessStatusCode)
                                        {
                                            throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                                        }
                                  
                                        YoutubeVideoPostResponse response = JsonConvert.DeserializeObject<YoutubeVideoPostResponse>(content);
                                        upload.VideoId = response.Id;

                                        //last stats update to reach 0 bytes and time left.
                                        upload.BytesSent = YoutubeVideoUploadService.stream.Position;
                                        resetEvent.WaitOne();
                                        upload.UploadStatus = UplStatus.Finished;
                                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload finished with resume position {lastResumePositionBeforeError}, package size {YoutubeVideoUploadService.stream.ReadBuffer}, video id {upload.VideoId}.");

                                        if (YoutubeVideoUploadService.stream.ReadBuffer != Settings.Instance.UserSettings.NetworkPackageSizeInBytes)
                                        {
                                            Tracer.Write($"YoutubeVideoUploadService.Upload: Real network package size different from configured package size, saving NetworkPackageSizeInBytes, before: {Settings.Instance.UserSettings.NetworkPackageSizeInBytes}, after: {YoutubeVideoUploadService.stream.ReadBuffer}.");
                                            //todo: maybe check/reset max upload after change
                                            Settings.Instance.UserSettings.NetworkPackageSizeInBytes = YoutubeVideoUploadService.stream.ReadBuffer;
                                            JsonSerializationSettings.JsonSerializer.SerializeSettings();
                                        }
                                    }
                                }
                            }
                            catch (TaskCanceledException e)
                            {
                                if (e.InnerException is TimeoutException)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: resume position {lastResumePositionBeforeError} try {uploadTry} upload timeout, current position {upload.BytesSent}.");
                                    upload.AddStatusInformation(StatusInformationCreator.Create("ERR0021", $"Timeout on upload resume position {lastResumePositionBeforeError} try {uploadTry}, current position {upload.BytesSent}."));
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }
                                else
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload stopped by user.");

                                    upload.BytesSent = YoutubeVideoUploadService.stream.Position;
                                    resetEvent.WaitOne();
                                    upload.UploadStatus = UplStatus.Stopped;

                                    YoutubeVideoUploadService.stream = null;
                                    return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, true);
                                }
                            }
                            catch (AuthenticationException e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Authentication exception: {e.ToString()}.");

                                StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0023", $"Could not upload.", e);
                                upload.AddStatusInformation(statusInformation);

                                error = true;
                                uploadTry++;
                            }
                            catch (HttpStatusException e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code resume position {lastResumePositionBeforeError} try {uploadTry}, current position {upload.BytesSent}: {e.StatusCode} {e.Message} with content {e.Content}.");
                                StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0024", $"Upload failed with resume position {lastResumePositionBeforeError} try {uploadTry}, current position {upload.BytesSent}.", e);

                                upload.AddStatusInformation(statusInformation);
                                if (statusInformation.IsQuotaError)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                                    resetEvent.WaitOne();
                                    upload.UploadStatus = UplStatus.Failed;
                                    return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                                }

                                error = true;
                                uploadTry++;
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpClient.SendAsync Exception resume position {lastResumePositionBeforeError.ToString("N0")} try {uploadTry}: {e.ToString()}.");
                                upload.AddStatusInformation(StatusInformationCreator.Create("ERR0027", $"Upload failed with resume position {lastResumePositionBeforeError.ToString("N0")} try {uploadTry}, current position {upload.BytesSent.ToString("N0")}.", e));
                                
                                error = true;
                                uploadTry++;
                            }
                        }
                    }
                    while(error);
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.AddStatusInformation(StatusInformationCreator.Create("ERR0028", "Upload failed.", e));
                
                resetEvent.WaitOne();
                upload.UploadStatus = UplStatus.Failed;

                YoutubeVideoUploadService.stream = null;
                return UploadResult.FailedWithoutDataSent;
            }

            YoutubeVideoUploadService.stream = null;
            Tracer.Write($"YoutubeVideoUploadService.Upload: End.");
            return UploadResult.Finished;
        }

        private static UploadResult getResult(long uploadBytesSent, long initialUploadByteSent, bool stopped)
        {
            if (uploadBytesSent - initialUploadByteSent > 1024 * 1024)
            {
                if (stopped)
                {
                    return UploadResult.StoppedWithDataSent;
                }
                
                return UploadResult.FailedWithDataSent;
            }

            if (stopped)
            {
                return UploadResult.StoppedWithoutDataSent;
            }

            return UploadResult.FailedWithoutDataSent;
        }

        private static async Task<bool> initializeUploadAsync(Upload upload, Action<Upload> resumableSessionUriSet, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: Start.");

            if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
            {
                Tracer.Write($"YoutubeVideoUploadService.initializeUploadAsync: Requesting new upload/new resumable session uri.");
                if (!await YoutubeVideoUploadService.requestNewUploadAsync(upload, resetEvent).ConfigureAwait(false))
                {
                    return false;
                }

                resumableSessionUriSet(upload);
            }
            else
            {
                Tracer.Write($"YoutubeVideoUploadService.initializeUploadAsync: Continue upload, getting range.");

                if (!await YoutubeVideoUploadService.getResumePositionAsync(upload, resetEvent).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: End.");
            return true;
        }

        private static async Task<bool> getResumePositionAsync(Upload upload, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Start");

            StringBuilder errors = new StringBuilder();
            short requestTry = 1;
            bool error = false;

            do
            {
                if (error)
                {
                    if (requestTry > 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: End, failed 3 times."); ;
                        upload.AddStatusInformation(StatusInformationCreator.Create("ERR0022", $"Getting resume position failed 3 times. Errors: {errors.ToString()}."));
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    error = false;

                    //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }

                try
                {
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Requesting resume position, try {requestTry}.");
                    StreamContent streamContent = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length);
                    using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                        upload.YoutubeAccount, HttpMethod.Put, upload.ResumableSessionUri).ConfigureAwait(false))
                    {
                        requestMessage.Content = streamContent;
                        using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                        {
                            string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (responseMessage.StatusCode != HttpStatusCode.PermanentRedirect)
                            {
                                throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                            }
                            else
                            {
                                Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Read header.");

                                try
                                {
                                    string range = responseMessage.Headers.GetValues("Range").First();
                                    if (!string.IsNullOrWhiteSpace(range))
                                    {
                                        string[] parts = range.Split('-');
                                        upload.BytesSent = Convert.ToInt64(parts[1]) + 1;
                                    }
                                }
                                catch (InvalidOperationException e)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Range header not found, restarting upload, exception: {e.ToString()}.");
                                    errors.AppendLine($"Youtube delivers no resume position information, restarting upload from beginning: {e.Message}.");
                                    upload.BytesSent = 0;
                                }
                                catch (Exception e)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Range header exception: {e.ToString()}.");
                                    errors.AppendLine($"Could not get resume position information: {e.GetType().Name}: {e.Message}.");

                                    error = true;
                                    requestTry++;
                                }
                            }
                        }
                    }
                }
                catch (AuthenticationException e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Authentication exception: {e.ToString()}.");

                    StatusInformationType statusInformationType = StatusInformationType.AuthenticationError;
                    if (e.IsApiResponseError)
                    {
                        statusInformationType |= StatusInformationType.AuthenticationApiResponseError;
                    }

                    StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0025", $"Could not get resume position.", e);
                    upload.AddStatusInformation(statusInformation);

                    error = true;
                    requestTry++;
                }
                catch (HttpStatusException e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                    StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0026", "Could not get resume position.", e);
                    upload.AddStatusInformation(statusInformation);
                    if (statusInformation.IsQuotaError)
                    {
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                        return false;
                    }

                    error = true;
                    requestTry++;
                }
                catch (Exception e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Exception: {e.ToString()}.");
                    errors.AppendLine($"YoutubeVideoUploadService.getResumePositionAsync: Getting upload index failed: {e.Message}.");

                    error = true;
                    requestTry++;
                }
            }
            while (error);

            return true;
        }

        private static async Task<bool> requestNewUploadAsync(Upload upload, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Start.");
            YoutubeVideoPostRequest video = new YoutubeVideoPostRequest();

            video.Snippet = new YoutubeVideoPostRequestSnippet();
            video.Snippet.Title = upload.Title;
            video.Snippet.Description = upload.Description;
            video.Snippet.Tags = upload.Tags != null ? upload.Tags.ToArray() : new List<string>().ToArray();
            video.Snippet.VideoLanguage = upload.VideoLanguage != null ? upload.VideoLanguage.Name.Replace('-', '_') : null; //works better with _, with - e.g. German (Switzerland) is not set at all, with _ it is set as German.
            video.Snippet.DescriptionLanguage = upload.DescriptionLanguage != null ? upload.DescriptionLanguage.Name.Replace('-', '_') : null; //works better with _, with - e.g. German (Switzerland) is not set at all, with _ it is set as German.
            video.Snippet.Category = null;
            if (upload.Category != null)
            {
                video.Snippet.Category = upload.Category.Id;
            }


            video.Status = new YoutubeVideoPostRequestStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt != null)
            {
                video.Status.PublishAt = upload.PublishAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string contentJson = JsonConvert.SerializeObject(video);

            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: New upload/new resumable session uri request content: {contentJson}.");

            FileInfo fileInfo = new FileInfo(upload.FilePath);
            //request upload session/uri

            //slug header adds original video file name to youtube studio, lambda filters to valid chars (ascii >=32 and <=255)
            string httpHeaderCompatibleString = new String(Path.GetFileName(upload.FilePath).Select(c =>
            {
                if ((c >= ' ' && c <= '~') || c == '\t')
                {
                    return c;
                }

                return '_';
            }).ToArray());

            short requestTry = 1;
            bool error = false;

            do
            {
                if (error)
                {
                    if (requestTry > 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, failed 3 times.");
                        upload.AddStatusInformation(StatusInformationCreator.Create("ERR0029", "Requesting new upload failed 3 times."));
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    error = false;

                    //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }

                try
                {
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload.");
                    using (ByteArrayContent byteArrayContent = HttpHelper.GetStreamContent(contentJson, "application/json"))
                    using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                        upload.YoutubeAccount, HttpMethod.Post, $"{YoutubeVideoUploadService.videoUploadEndpoint}?part=snippet,status&uploadType=resumable").ConfigureAwait(false))
                    {
                        byteArrayContent.Headers.Add("Slug", httpHeaderCompatibleString);
                        byteArrayContent.Headers.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
                        byteArrayContent.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));
                        requestMessage.Content = byteArrayContent;

                        using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                        {
                            string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!responseMessage.IsSuccessStatusCode)
                            {
                                throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                            }

                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Read header.");
                            upload.ResumableSessionUri = responseMessage.Headers.GetValues("Location").First();
                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End.");
                        }
                    }
                }
                catch (AuthenticationException e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Authentication exception: {e.ToString()}.");
                    upload.AddStatusInformation(StatusInformationCreatorYoutube.Create("ERR0030", "Could not request new upload.", e));

                    error = true;
                    requestTry++;
                }
                catch (HttpStatusException e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                    StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0031", "Could not request new upload.", e);
                    upload.AddStatusInformation(statusInformation);
                    if (statusInformation.IsQuotaError)
                    {
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                        return false;
                    }

                    error = true;
                    requestTry++;
                }
                catch (Exception e)
                {
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Exception: {e.ToString()}.");
                    upload.AddStatusInformation(StatusInformationCreator.Create("ERR0032", "Could not request new upload.", e));

                    error = true;
                    requestTry++;
                }

            } while (error);

            return true;
        }
    }
}
