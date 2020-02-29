using Drexel.VidUp.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Drexel.VidUp.JSON
{
    public class TemplateGuidStringConverter : JsonConverter<Template>
    {
        public override Template ReadJson(JsonReader reader, Type objectType, Template existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return null; ;
        }

        public override void WriteJson(JsonWriter writer, Template value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Guid);
        }
    }
}
