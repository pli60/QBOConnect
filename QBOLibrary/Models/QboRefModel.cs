using Newtonsoft.Json;

namespace QBOLibrary.Models
{
    // QBO reference type - { "value": "1", "name": "Amy's Bird Sanctuary" }
    public class QboRefModel
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        public QboRefModel()
        {
        }

        public QboRefModel(string value)
        {
            Value = value;
        }

        public QboRefModel(string value, string name)
        {
            Value = value;
            Name = name;
        }
    }
}
