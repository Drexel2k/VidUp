using System;
using System.Runtime.Serialization;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class YoutubeAccountNameStringConverter : JsonConverter<YoutubeAccount>
    {
        public override YoutubeAccount ReadJson(JsonReader reader, Type objectType, YoutubeAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string accountName = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(accountName))
            {
                throw new SerializationException("Youtube Account not found.");
            }

            YoutubeAccount account = DeserializationRepositoryContent.YoutubeAccountList.Find(acc => acc.Name == accountName);
            if (account == null)
            {
                throw new SerializationException("Youtube Account not found.");
            }

            return account;
        }

        public override void WriteJson(JsonWriter writer, YoutubeAccount value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Name);
        }
    }
}
