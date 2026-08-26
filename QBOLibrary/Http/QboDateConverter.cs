using System;
using System.Globalization;
using Newtonsoft.Json;

namespace QBOLibrary.Http
{
    // QBO transaction dates are date-only. Sending a full timestamp is rejected.
    public class QboDateConverter : JsonConverter
    {
        private const string Format = "yyyy-MM-dd";

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTime) || objectType == typeof(DateTime?);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteValue(((DateTime)value).ToString(Format, CultureInfo.InvariantCulture));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            if (reader.TokenType == JsonToken.Date)
            {
                return reader.Value;
            }

            string text = reader.Value?.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            DateTime parsed;
            if (DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture,
                                       DateTimeStyles.None, out parsed))
            {
                return parsed;
            }
            if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
                                  DateTimeStyles.None, out parsed))
            {
                return parsed;
            }
            return null;
        }
    }
}
