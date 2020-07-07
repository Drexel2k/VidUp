using System;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json
{
    public class MonthRelativeCombinationConverter : JsonConverter<MonthRelativeCombination>
    {

        public override MonthRelativeCombination ReadJson(JsonReader reader, Type objectType, MonthRelativeCombination existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string guidString = (string)reader.Value;
            if (string.IsNullOrWhiteSpace(guidString))
            {
                return null;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, MonthRelativeCombination value, JsonSerializer serializer)
        {
            
        }
    }
}
