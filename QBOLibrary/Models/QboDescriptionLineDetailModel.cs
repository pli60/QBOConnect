using System;
using Newtonsoft.Json;
using QBOLibrary.Http;

namespace QBOLibrary.Models
{
    public class QboDescriptionLineDetailModel
    {
        [JsonConverter(typeof(QboDateConverter))]
        public DateTime? ServiceDate { get; set; }

        public QboRefModel TaxCodeRef { get; set; }
    }
}