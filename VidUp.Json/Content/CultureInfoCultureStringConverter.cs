using System;
using System.Globalization;
using System.Linq;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class CultureInfoCultureStringConverter : JsonConverter<CultureInfo>
    {
        public override CultureInfo ReadJson(JsonReader reader, Type objectType, CultureInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            //must be AllCultureInfos as culture can be set on template before cultures were filtered in settings.
            //So in a template e.g. can be a culture which is later filtered out.
            return Cultures.AllCultureInfos.Where(culture => culture.Name == (string)reader.Value).First();
        }

        public override void WriteJson(JsonWriter writer, CultureInfo value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Name);
        }
    }
}
