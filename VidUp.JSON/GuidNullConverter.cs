#region

using System;
using System.Runtime.Remoting.Messaging;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.JSON
{
    public class GuidNullConverter : JsonConverter<Guid>
    {
        public override Guid ReadJson(JsonReader reader, Type objectType, Guid existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string guid = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(guid))
            {
                return Guid.Empty;
            }

            return Guid.Parse(guid);
        }

        public override void WriteJson(JsonWriter writer, Guid value, JsonSerializer serializer)
        {
            if (value == Guid.Empty)
            {
                writer.WriteNull();

            }
            else
            {
                writer.WriteValue(value.ToString());
            }
        }
    }
}
