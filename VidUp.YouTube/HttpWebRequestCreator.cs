using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Drexel.VidUp.Youtube
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

            HttpWebRequest request =  await HttpWebRequestCreator.createBasicUploadHttpWebRequest(endpoint, httpMethod, fileInfo.Length);
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


            HttpWebRequest request = await HttpWebRequestCreator.createBasicUploadHttpWebRequest(endpoint, httpMethod, bytes.Length);
            request.ContentLength = bytes.Length;
            request.ContentType = contentType;

            return request;
        }

        private static async Task<HttpWebRequest> createBasicUploadHttpWebRequest(string endpoint, string httpMethod, long length)
        {
            string accessToken = await YoutubeAuthentication.GetAccessToken();

            Uri uri = new Uri(endpoint);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = httpMethod;
            request.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + accessToken);

            //raise limits for large uploads
            if (length > 100 * 1024 * 1024)
            {
                request.Timeout = int.MaxValue;
                request.AllowWriteStreamBuffering = false;
                request.ServicePoint.SetTcpKeepAlive(true, 10000, 1000);
            }

            return request;
        }
    }
}
