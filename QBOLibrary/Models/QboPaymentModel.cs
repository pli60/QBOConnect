using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QBOLibrary.Http;

namespace QBOLibrary.Models
{
    public class QboPaymentModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }

        [JsonConverter(typeof(QboDateConverter))]
        public DateTime? TxnDate { get; set; }

        public QboRefModel CustomerRef { get; set; }
        public QboRefModel PaymentMethodRef { get; set; }
        public QboRefModel DepositToAccountRef { get; set; }
        public QboRefModel ARAccountRef { get; set; }

        public string PaymentRefNum { get; set; }
        public string PrivateNote { get; set; }

        public decimal? TotalAmt { get; set; }
        public decimal? UnappliedAmt { get; set; }

        public List<QboPaymentLineModel> Line { get; set; }

        [JsonProperty("sparse")]
        public bool? Sparse { get; set; }

        public QboMetaDataModel MetaData { get; set; }
    }
}
