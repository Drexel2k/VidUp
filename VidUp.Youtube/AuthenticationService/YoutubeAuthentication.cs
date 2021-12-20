using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Http;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.AuthenticationService
{
    //based on: https://github.com/googlesamples/oauth-apps-for-windows/blob/master/OAuthDesktopApp/OAuthDesktopApp/MainWindow.xaml.cs
    public static class YoutubeAuthentication
    {
        private static string authorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private static string tokenEndpoint = "https://www.googleapis.com/oauth2/v4/token";

        private static string scopes = "https://www.googleapis.com/auth/youtube";

        private static HttpClient client;

        static YoutubeAuthentication()
        {
            YoutubeAuthentication.client = new HttpClient();
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            YoutubeAuthentication.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        }

        //get access token from API
        public static async Task<AccessToken> GetNewAccessTokenAsync(YoutubeAccount youtubeAccount)
        {
            Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: Start.");

            try
            {
                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: Checking refresh token.");
                //check for refresh token, if not there, get it
                if (string.IsNullOrWhiteSpace(youtubeAccount.RefreshToken))
                {
                    Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: No refresh token for account, requesting refresh token.");
                    await YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync(youtubeAccount).ConfigureAwait(false);
                }

                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: Refresh token found.");
                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: Requesting access token.");
                string clientId = string.IsNullOrWhiteSpace(Settings.Instance.UserSettings.ClientId) ? Credentials.ClientId : Settings.Instance.UserSettings.ClientId;
                string clientSecret = string.IsNullOrWhiteSpace(Settings.Instance.UserSettings.ClientSecret) ? Credentials.ClientSecret : Settings.Instance.UserSettings.ClientSecret;
                //here we should have the refresh token
                // builds the  request
                string tokenRequestBody = $"refresh_token={youtubeAccount.RefreshToken}&client_id={clientId}&client_secret={clientSecret}&grant_type=refresh_token";

                // sends the request
                byte[] contentBytes = Encoding.ASCII.GetBytes(tokenRequestBody);
                using (ByteArrayContent byteArrayContent = new ByteArrayContent(contentBytes))
                {
                    byteArrayContent.Headers.ContentLength = contentBytes.Length;
                    byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    using (HttpResponseMessage resposneMessage = await YoutubeAuthentication.client.PostAsync(YoutubeAuthentication.tokenEndpoint, byteArrayContent).ConfigureAwait(false))
                    {
                        string content = await resposneMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!resposneMessage.IsSuccessStatusCode)
                        {
                            throw new HttpStatusException(resposneMessage.ReasonPhrase, (int)resposneMessage.StatusCode, content);
                        }

                        Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(await resposneMessage.Content.ReadAsStringAsync().ConfigureAwait(false));

                        AccessToken accessToken = new AccessToken(tokenEndpointDecoded["access_token"], DateTime.Now.AddSeconds(Convert.ToInt32(tokenEndpointDecoded["expires_in"])));

                        Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: End.");
                        return accessToken;
                    }
                }
            }
            catch (HttpStatusException e)
            {
                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: End.");
                throw new AuthenticationException($"HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.", e, true);
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeAuthentication.GetNewAccessTokenAsync: End, Unexpected Exception: {e.ToString()}.");
                throw new AuthenticationException($"Error on getting access token: {e.Message}", e, false);
            }
        }

        //initial authentication, get refresh token from API
        public static async Task SetRefreshTokenOnYoutubeAccountAsync(YoutubeAccount youtubeAccount)
        {
            Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Start.");
            // Generates state and PKCE values.

            try
            {
                string state = YoutubeAuthentication.randomDataBase64url(32);
                string codeVerifier = YoutubeAuthentication.randomDataBase64url(32);
                string codeChallenge = YoutubeAuthentication.base64urlencodeNoPadding(YoutubeAuthentication.sha256(codeVerifier));
                const string codeChallengeMethod = "S256";

                // Creates a redirect URI using an available port on the loopback address.
                string redirectUri = $"http://{IPAddress.Loopback}:{YoutubeAuthentication.getRandomUnusedPort()}/";

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Starting http server.");
                // Creates an HttpListener to listen for requests on that redirect URI.
                HttpListener http = new HttpListener();
                http.Prefixes.Add(redirectUri);
                http.Start();

                string clientId = string.IsNullOrWhiteSpace(Settings.Instance.UserSettings.ClientId) ? Credentials.ClientId : Settings.Instance.UserSettings.ClientId;
                string clientSecret = string.IsNullOrWhiteSpace(Settings.Instance.UserSettings.ClientSecret) ? Credentials.ClientSecret : Settings.Instance.UserSettings.ClientSecret;

                // Creates the OAuth 2.0 authorization request.
                string authorizationRequest = $"{YoutubeAuthentication.authorizationEndpoint}?response_type=code&scope={Uri.EscapeDataString(YoutubeAuthentication.scopes)}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={clientId}&state={state}&code_challenge={codeChallenge}&code_challenge_method={codeChallengeMethod}";

                ProcessStartInfo processStartInfo = new ProcessStartInfo(authorizationRequest)
                {
                    UseShellExecute = true,
                    Verb = "open"
                };

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Opening browser.");
                // Opens request in the browser.
                Process.Start(processStartInfo);
                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Waiting for user input.");
                // Waits for the OAuth authorization response.
                HttpListenerContext context = await http.GetContextAsync().ConfigureAwait(false);

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Sending response.");
                // Sends an HTTP response to the browser.
                HttpListenerResponse response = context.Response;
                string responseString = "<html><head><meta http-equiv='refresh' content='10;url=https://google.com'></head><body>Please return to the app.</body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                Stream responseOutput = response.OutputStream;
                responseOutput.Write(buffer, 0, buffer.Length);
                responseOutput.Close();
                http.Stop();

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Error checking.");
                // Checks for errors.
                if (context.Request.QueryString.Get("error") != null)
                {
                    string error = $"Authentication error: {context.Request.QueryString.Get("error")}";
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: {error}.");
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End.");
                    throw new AuthenticationException($"Error returned: {error}", true);
                }
                if (context.Request.QueryString.Get("code") == null || context.Request.QueryString.Get("state") == null)
                {
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Authentication error: Code or State is null.");
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End.");
                    throw new AuthenticationException("Code or State is null.", true);
                }

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: No errors.");
                // extracts the code
                string code = context.Request.QueryString.Get("code");
                string incomingState = context.Request.QueryString.Get("state");

                // Compares the receieved state to the expected value, to ensure that
                // this app made the request which resulted in authorization.

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Checking state.");
                if (incomingState != state)
                {
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Authentication error: Code and State differ.");
                    Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End.");
                    throw new AuthenticationException("Code and State differ.", true);
                }

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: State ok.");
                // Starts the code exchange for refresh token at the token endpoint.
                // builds the  request
                string tokenRequestBody = $"code={code}&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={Credentials.ClientId}&code_verifier={codeVerifier}&client_secret={clientSecret}&scope=&grant_type=authorization_code";

                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: Getting refresh token.");
                // sends the request
                byte[] contentBytes = Encoding.ASCII.GetBytes(tokenRequestBody);
                using (ByteArrayContent byteArrayContent = new ByteArrayContent(contentBytes))
                {
                    byteArrayContent.Headers.ContentLength = contentBytes.Length;
                    byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                    using (HttpResponseMessage responseMessage = await YoutubeAuthentication.client.PostAsync(YoutubeAuthentication.tokenEndpoint, byteArrayContent).ConfigureAwait(false))
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                        }

                        Dictionary<string, string> tokenEndpointDecoded = JsonConvert.DeserializeObject<Dictionary<string, string>>(await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
                        youtubeAccount.RefreshToken = tokenEndpointDecoded["refresh_token"];
                        JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();
                    }
                }
            }
            catch (HttpStatusException e)
            {
                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End.");
                throw new AuthenticationException($"HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.", e, true);
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End, Unexpected Exception: {e.ToString()}.");
                throw new AuthenticationException($"Error on getting refresh token: {e.Message}", e, false);
            }

            Tracer.Write($"YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync: End.");
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
            return YoutubeAuthentication.base64urlencodeNoPadding(bytes);
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
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
