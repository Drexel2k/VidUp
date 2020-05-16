#region

using System;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.JSON
{
    public class UploadGuidStringConverter : JsonConverter<Upload>
    {

        public override Upload ReadJson(JsonReader reader, Type objectType, Upload existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string guidString = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(guidString))
            {
                return null;
            }

            return DeSerializationRepository.AllUploads.Find(upload => upload.Guid == Guid.Parse(guidString));
        }

        public override void WriteJson(JsonWriter writer, Upload value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Guid);
        }
    }
}
