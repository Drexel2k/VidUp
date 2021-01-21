using System;
using System.Linq;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class CategoryIdConverter : JsonConverter<Category>
    {
        public override Category ReadJson(JsonReader reader, Type objectType, Category existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            return Category.Categories.Where(category => category.Id == (long)reader.Value).First();
        }

        public override void WriteJson(JsonWriter writer, Category value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Id);
        }
    }
}
