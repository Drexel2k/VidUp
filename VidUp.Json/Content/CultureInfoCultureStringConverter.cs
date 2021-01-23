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

            return Cultures.RelevantCultureInfos.Where(culture => culture.Name == (string)reader.Value).First();
        }

        public override void WriteJson(JsonWriter writer, CultureInfo value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Name);
        }
    }
}
