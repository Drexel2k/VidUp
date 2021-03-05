using System;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class TemplateGuidStringConverter : JsonConverter<Template>
    {
        public override Template ReadJson(JsonReader reader, Type objectType, Template existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return null;
        }

        public override void WriteJson(JsonWriter writer, Template value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Guid);
        }
    }
}
