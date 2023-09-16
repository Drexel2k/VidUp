using System;
using System.Runtime.Serialization;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class YoutubeAccountGuidStringConverter : JsonConverter<YoutubeAccount>
    {
        public override YoutubeAccount ReadJson(JsonReader reader, Type objectType, YoutubeAccount existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string guid = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(guid))
            {
                throw new SerializationException("Youtube Account not found.");
            }

            YoutubeAccount account = DeserializationRepositoryContent.YoutubeAccountList.Find(acc => acc.Guid == Guid.Parse(guid));
            if (account == null)
            {
                JsonDeserializationContent.AllYoutubeAccountsDeserialiozed = false;
                return JsonDeserializationContent.YoutubeAccountForNullReplacement;
            }

            return account;
        }

        public override void WriteJson(JsonWriter writer, YoutubeAccount value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Guid);
        }
    }
}
