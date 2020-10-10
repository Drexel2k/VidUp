using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace Drexel.VidUp.Youtube.Service
{
    public static class HttpWebRequestCreator
    {
        public static async Task<HttpWebRequest> CreateAuthenticatedUploadHttpWebRequest(string endpoint, string httpMethod, string file)
        {
            if(string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint");
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                throw new ArgumentException("httpMethod");
            }

            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                throw new ArgumentException("file");
            }

            FileInfo fileInfo = new FileInfo(file);

            HttpWebRequest request =  await HttpWebRequestCreator.createBasicHttpWebRequest(endpoint, httpMethod);
            request.ContentLength = fileInfo.Length;
            request.ContentType = MimeMapping.GetMimeMapping(file); ;

            return request;
        }

        public static async Task<HttpWebRequest> CreateAuthenticatedUploadHttpWebRequest(string endpoint, string httpMethod, byte[] bytes, string contentType)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint");
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                throw new ArgumentException("httpMethod");
            }

            if (string.IsNullOrWhiteSpace(contentType))
            {
                throw new ArgumentException("contentType");
            }

            if (bytes == null)
            {
                throw new ArgumentException("bytes");
            }


            HttpWebRequest request = await HttpWebRequestCreator.createBasicHttpWebRequest(endpoint, httpMethod);
            request.ContentLength = bytes.Length;
            request.ContentType = contentType;

            return request;
        }

        private static async Task<HttpWebRequest> createBasicHttpWebRequest(string endpoint, string httpMethod)
        {
            string accessToken = await YoutubeAuthentication.GetAccessToken();

            Uri uri = new Uri(endpoint);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = httpMethod;
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken);
            request.AllowWriteStreamBuffering = false;

            return request;
        }

        public static async Task<HttpWebRequest> CreateAuthenticatedResumeInformationHttpWebRequest(string endpoint, string httpMethod, string file)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint");
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                throw new ArgumentException("httpMethod");
            }

            if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            {
                throw new ArgumentException("file");
            }

            FileInfo fileInfo = new FileInfo(file);

            HttpWebRequest request = await HttpWebRequestCreator.createBasicHttpWebRequest(endpoint, httpMethod);
            request.ContentLength = 0;
            request.Headers.Add("Content-Range", string.Format("bytes */{0}",fileInfo.Length.ToString()));

            return request;
        }

        public static async Task<HttpWebRequest> CreateAuthenticatedResumeHttpWebRequest(string endpoint, string httpMethod, long uploadFileSize, long startByteIndex, int chunkSize)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint");
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                throw new ArgumentException("httpMethod");
            }

            long lastByteIndex;
            if (startByteIndex + chunkSize >= uploadFileSize)
            {
                lastByteIndex = uploadFileSize - 1;
            }
            else
            {
                lastByteIndex = startByteIndex + chunkSize - 1;
            }

            string rangeString = string.Format("bytes {0}-{1}/{2}", startByteIndex, lastByteIndex, uploadFileSize);

            HttpWebRequest request = await HttpWebRequestCreator.createBasicHttpWebRequest(endpoint, httpMethod);
            request.ContentLength = lastByteIndex - startByteIndex + 1;
            request.Headers.Add("Content-Range", rangeString);

            return request;
        }

        public static async Task<HttpWebRequest> CreateAuthenticatedHttpWebRequest(string endpoint, string httpMethod)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("endpoint");
            }

            if (string.IsNullOrWhiteSpace(httpMethod))
            {
                throw new ArgumentException("httpMethod");
            }

            HttpWebRequest request = await HttpWebRequestCreator.createBasicHttpWebRequest(endpoint, httpMethod);
            request.ContentLength = 0;

            return request;
        }
    }
}
