using System;
using System.Runtime.Serialization;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class PlaylistPlaylistIdConverter : JsonConverter<Playlist>
    {

        public override Playlist ReadJson(JsonReader reader, Type objectType, Playlist existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string playlistId = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(playlistId))
            {
                return null;
            }

            Playlist playlist = DeserializationRepositoryContent.PlaylistList.Find(playlist => playlist.PlaylistId == playlistId);
            if (playlist == null)
            {
                JsonDeserializationContent.AllPlaylistsDeserialiozed = false;
            }

            return playlist;
        }

        public override void WriteJson(JsonWriter writer, Playlist value, JsonSerializer serializer)
        {
            writer.WriteValue(value.PlaylistId);
        }
    }
}
