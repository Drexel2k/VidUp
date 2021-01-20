using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace Drexel.VidUp.Youtube.Service
{
    public static class HttpHelper
    {
        private static HttpClient standardClient;
        private static HttpClient uploadClient;

        private static DateTime accessTokenExpiry = DateTime.MinValue;
        private static TimeSpan oneMinute = new TimeSpan(0, 1, 0);
        static HttpHelper()
        {

            HttpHelper.standardClient  = new HttpClient();

            HttpHelper.uploadClient = new HttpClient();
            HttpHelper.uploadClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        //buffer is set to constant value like 1024, 4096, 16384 etc.
        //eg. if set to 10240 buffer will be 16384.
        private static int bufferSize = 16 * 1024;

        public static async Task<HttpClient> GetAuthenticatedStandardClient()
        {
            await HttpHelper.checkAccesToken();
            return HttpHelper.standardClient;
        }

        public static async Task<HttpClient> GetAuthenticatedUploadClient()
        {
            await HttpHelper.checkAccesToken();
            return HttpHelper.uploadClient;
        }

        private static async Task checkAccesToken()
        {
            if (HttpHelper.accessTokenExpiry - DateTime.Now < oneMinute)
            {
                AccessToken token = await YoutubeAuthentication.GetNewAccessToken();
                HttpHelper.accessTokenExpiry = token.Expiry;
                HttpHelper.standardClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
                HttpHelper.uploadClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
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

        public static StreamContent GetStreamContentResumableUpload(Stream data, long orirginalLength, long startByteIndex, int chunkSize, string contentType)
        {
            long lastByteIndex;
            if (startByteIndex + chunkSize >= orirginalLength)
            {
                lastByteIndex = orirginalLength - 1;
            }
            else
            {
                lastByteIndex = startByteIndex + chunkSize - 1;
            }

            string rangeString = string.Format("bytes {0}-{1}/{2}", startByteIndex, lastByteIndex, orirginalLength);

            StreamContent content = new StreamContent(data, HttpHelper.bufferSize);
            content.Headers.ContentLength = lastByteIndex - startByteIndex + 1;
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Headers.Add("Content-Range", rangeString);

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
