using System;

namespace Drexel.VidUp.Youtube.Playlist
{
    public class PlaylistApi
    {
        private string id;
        private string title;
        public string Id { get => this.id; }

        public string Title { get => this.title; }

        public PlaylistApi(string id, string title)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Id must not be null or empty.");
            }

            this.id = id;
            this.title = title;
        }
    }
}