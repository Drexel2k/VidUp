using System;

namespace Drexel.VidUp.Youtube
{
    public class AccessToken
    {
        private string token;
        private DateTime expiry;

        public string Token { get => this.token; }
        public DateTime Expiry { get => this.expiry; }

        public AccessToken(string token, DateTime expiry)
        {
            this.token = token;
            this.expiry = expiry;
        }
    }
}