using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.AuthenticationService;

namespace Drexel.VidUp.Youtube
{
    public static class HttpHelper
    {
        private static HttpClient standardClient;
        private static HttpClient uploadClient;

        private static Dictionary<YoutubeAccount, AccessToken> youtubeAccessTokenExpiryByAccount = new Dictionary<YoutubeAccount, AccessToken>();
        private static TimeSpan oneMinute = new TimeSpan(0, 1, 0);
        static HttpHelper()
        {

            HttpHelper.standardClient  = new HttpClient();

            HttpHelper.uploadClient = new HttpClient();
            HttpHelper.uploadClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        //buffer is set to constant value like 1024, 4096, 16384 etc.
        //eg. if set to 10240 buffer will be 16384.
        private static int bufferSize = 32 * 1024;

        public static HttpClient StandardClient
        {
            get => HttpHelper.standardClient;
        }

        public static HttpClient UploadClient
        {
            get => HttpHelper.uploadClient;
        }

        public static async Task<HttpRequestMessage> GetAuthenticatedRequestMessageAsync(YoutubeAccount youtubeAccount, HttpMethod method, string uri)
        {
            await HttpHelper.ensureAccessTokenAsync(youtubeAccount).ConfigureAwait(false);
            HttpRequestMessage message = new HttpRequestMessage(method, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", HttpHelper.youtubeAccessTokenExpiryByAccount[youtubeAccount].Token);
            return message;
        }

        public static async Task<HttpRequestMessage> GetAuthenticatedRequestMessageAsync(YoutubeAccount youtubeAccount, HttpMethod method)
        {
            await HttpHelper.ensureAccessTokenAsync(youtubeAccount).ConfigureAwait(false);
            HttpRequestMessage message = new HttpRequestMessage();
            message.Method = method;
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", HttpHelper.youtubeAccessTokenExpiryByAccount[youtubeAccount].Token);
            return message;
        }

        private static async Task ensureAccessTokenAsync(YoutubeAccount youtubeAccount)
        {
            if (!HttpHelper.youtubeAccessTokenExpiryByAccount.ContainsKey(youtubeAccount) || HttpHelper.youtubeAccessTokenExpiryByAccount[youtubeAccount].Expiry - DateTime.Now < oneMinute)
            {
                AccessToken token = await YoutubeAuthentication.GetNewAccessTokenAsync(youtubeAccount).ConfigureAwait(false);
                HttpHelper.youtubeAccessTokenExpiryByAccount[youtubeAccount] = token;
            }
        }

        public static StreamContent GetStreamContentContentRangeOnly(long length)
        {
            StreamContent content = new StreamContent(Stream.Null);
            content.Headers.ContentLength = 0;
            content.Headers.Add("Content-Range", string.Format("bytes */{0}", length.ToString()));

            return content;
        }

        public static ByteArrayContent GetStreamContent(string content, string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("contentType");
            }

            byte[] bytes = content == null ? new byte[0] : Encoding.UTF8.GetBytes(content);
            ByteArrayContent streamContent = new ByteArrayContent(bytes);
            streamContent.Headers.ContentLength = bytes.Length;
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            return streamContent;
        }

        public static StreamContent GetStreamContentResumableUpload(Stream data, long orirginalLength, long startByteIndex, string contentType)
        {
            StreamContent content = new StreamContent(data, HttpHelper.bufferSize);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            if (startByteIndex <= 0)
            {
                content.Headers.ContentLength = orirginalLength;
            }
            else
            {
                long lastByteIndex = orirginalLength - 1;
                string rangeString = string.Format("bytes {0}-{1}/{2}", startByteIndex, lastByteIndex, orirginalLength);
                content.Headers.ContentLength = lastByteIndex - startByteIndex + 1;
                content.Headers.Add("Content-Range", rangeString);
            }

            return content;
        }

        public static StreamContent GetStreamContentUpload(Stream data, string contentType)
        {
            StreamContent content = new StreamContent(data, HttpHelper.bufferSize);
            content.Headers.ContentLength = data.Length;
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            return content;
        }
    }
}
