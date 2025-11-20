using Newtonsoft.Json;
using System;

namespace SINEATER.Localization
{
    internal class LocalizedStringConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LocalizedString);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            Enum.TryParse(typeof(LocaIDs), (string)reader.Value, out object x);
            return new LocalizedString((LocaIDs)x);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer.WriteValue(((LocalizedString)value).ID.ToString());
        }
    }
}
