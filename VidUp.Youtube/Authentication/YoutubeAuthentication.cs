using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Authentication
{
    //based on: https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
    public static class YoutubeAuthentication
    {
        private static string refreshToken;
        private static string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private static string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

        private static string scopes = "https://www.googleapis.com/auth/youtube";
        private static string serializationFolder;

        private static HttpClient client;

        static YoutubeAuthentication()
        {
            YoutubeAuthentication.client = new HttpClient();
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        }
        public static string SerializationFolder
        {
            set
            {
                YoutubeAuthentication.serializationFolder = value;
            }
        }
        private static string getRefreshTokenFilePath()
        {
            return string.Format(@"{0}\uploadrefreshtoken", YoutubeAuthentication.serializationFolder);
        }

        //get access token from API
        public static async Task<AccessToken> GetNewAccessTokenAsync()
        {
            //check for refresh token, if not there, get it
            if (string.IsNullOrWhiteSpace(YoutubeAuthentication.refreshToken))
            {
                if (!YoutubeAuthentication.trySetRefreshToken())
                {
                    await YoutubeAuthentication.getRefreshTokenAsync().ConfigureAwait(false);
                }
            }

            //here we should have the refresh token
            // builds the  request
            string tokenRequestBody = string.Format(
                "refresh_token={0}&client_id={1}&client_secret={2}&grant_type=refresh_token",
                YoutubeAuthentication.refreshToken,
                Credentials.ClientId,
                Credentials.ClientSecret
            );

            // sends the request

            byte[] contentBytes = Encoding.ASCII.GetBytes(tokenRequestBody);
            using (ByteArrayContent content = new ByteArrayContent(contentBytes))
            {
                content.Headers.ContentLength = contentBytes.Length;
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using (HttpResponseMessage message = await YoutubeAuthentication.client.PostAsync(YoutubeAuthentication.tokenEndpoint, content).ConfigureAwait(false))
                {
                    message.EnsureSuccessStatusCode();
                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(await message.Content.ReadAsStringAsync().ConfigureAwait(false));

                    AccessToken accessToken = new AccessToken(tokenEndpointDecoded["access_token"],
                        DateTime.Now.AddSeconds(Convert.ToInt32(tokenEndpointDecoded["expires_in"])));
                    return accessToken;
                }
            }
        }

        //get refresh token from file
        private static bool trySetRefreshToken()
        {
            string refreshToken;
            string refreshTokenFilePath = YoutubeAuthentication.getRefreshTokenFilePath();
            if (File.Exists(refreshTokenFilePath))
            {
                refreshToken = File.ReadAllText(refreshTokenFilePath);
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return false;
                }
                else
                {
                    YoutubeAuthentication.refreshToken = refreshToken;
                    return true;
                }
            }
            else
            {
                return false;
            }
        }

        //initial authentication, get refresh token from API
        private static async Task getRefreshTokenAsync()
        {
            // Generates state and PKCE values.
            string state = randomDataBase64url(32);
            string code_verifier = randomDataBase64url(32);
            string code_challenge = base64urlencodeNoPadding(sha256(code_verifier));
            const string code_challenge_method = "S256";

            // Creates a redirect URI using an available port on the loopback address.
            string redirectURI = string.Format("http://{0}:{1}/", IPAddress.Loopback, YoutubeAuthentication.getRandomUnusedPort());

            // Creates an HttpListener to listen for requests on that redirect URI.
            var http = new HttpListener();
            http.Prefixes.Add(redirectURI);
            http.Start();

            // Creates the OAuth 2.0 authorization request.
            string authorizationRequest = string.Format("{0}?response_type=code&scope={6}&redirect_uri={1}&client_id={2}&state={3}&code_challenge={4}&code_challenge_method={5}",
                YoutubeAuthentication.authorizationEndpoint,
                Uri.EscapeDataString(redirectURI),
                Credentials.ClientId,
                state,
                code_challenge,
                code_challenge_method,
                Uri.EscapeDataString(YoutubeAuthentication.scopes));

            var ps = new ProcessStartInfo(authorizationRequest)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            // Opens request in the browser.
            Process.Start(ps);

            // Waits for the OAuth authorization response.
            var context = await http.GetContextAsync().ConfigureAwait(false);

            // Sends an HTTP response to the browser.
            var response = context.Response;
            string responseString = string.Format("<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>");
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            var responseOutput = response.OutputStream;
            responseOutput.Write(buffer, 0, buffer.Length);
            responseOutput.Close();
            http.Stop();

            // Checks for errors.
            if (context.Request.QueryString.Get("error") != null)
            {
                throw new AuthenticationException(string.Format("Authentication error: {0}", context.Request.QueryString.Get("error")));
            }
            if (context.Request.QueryString.Get("code") == null
                || context.Request.QueryString.Get("state") == null)
            {
                throw new AuthenticationException("Authentication error: Code or State is null.");
            }

            // extracts the code
            var code = context.Request.QueryString.Get("code");
            var incoming_state = context.Request.QueryString.Get("state");

            // Compares the receieved state to the expected value, to ensure that
            // this app made the request which resulted in authorization.
            if (incoming_state != state)
            {
                throw new AuthenticationException("Authentication error: Code and State differ.");
            }

            // Starts the code exchange for refresh token at the token endpoint.
            // builds the  request
            string tokenRequestBody = string.Format("code={0}&redirect_uri={1}&client_id={2}&code_verifier={3}&client_secret={4}&scope=&grant_type=authorization_code",
                code,
                System.Uri.EscapeDataString(redirectURI),
                Credentials.ClientId,
                code_verifier,
                Credentials.ClientSecret
                );

            // sends the request
            byte[] contentBytes = Encoding.ASCII.GetBytes(tokenRequestBody);
            using (ByteArrayContent content = new ByteArrayContent(contentBytes))
            {
                content.Headers.ContentLength = contentBytes.Length;
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using (HttpResponseMessage message = await YoutubeAuthentication.client.PostAsync(YoutubeAuthentication.tokenEndpoint, content).ConfigureAwait(false))
                {
                    message.EnsureSuccessStatusCode();

                    Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(await message.Content.ReadAsStringAsync().ConfigureAwait(false));
                    YoutubeAuthentication.refreshToken = tokenEndpointDecoded["refresh_token"];
                    File.WriteAllText(YoutubeAuthentication.getRefreshTokenFilePath(), YoutubeAuthentication.refreshToken);
                }
            }
        }

        /// <summary>
        /// Returns URI-safe data with a given input length.
        /// </summary>
        /// <param name="length">Input length (nb. output will be longer)</param>
        /// <returns></returns>
        public static string randomDataBase64url(uint length)
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] bytes = new byte[length];
            rng.GetBytes(bytes);
            return base64urlencodeNoPadding(bytes);
        }

        /// <summary>
        /// Returns the SHA256 hash of the input string.
        /// </summary>
        /// <param name="inputStirng"></param>
        /// <returns></returns>
        public static byte[] sha256(string inputStirng)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(inputStirng);
            SHA256Managed sha256 = new SHA256Managed();
            return sha256.ComputeHash(bytes);
        }

        /// <summary>
        /// Base64url no-padding encodes the given input buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static string base64urlencodeNoPadding(byte[] buffer)
        {
            string base64 = Convert.ToBase64String(buffer);

            // Converts base64 to base64url.
            base64 = base64.Replace("+", "-");
            base64 = base64.Replace("/", "_");
            // Strips padding.
            base64 = base64.Replace("=", "");

            return base64;
        }

        private static int getRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
