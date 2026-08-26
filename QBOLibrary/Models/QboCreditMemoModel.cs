using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QBOLibrary.Http;

namespace QBOLibrary.Models
{
    public class QboCreditMemoModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string DocNumber { get; set; }

        [JsonConverter(typeof(QboDateConverter))]
        public DateTime? TxnDate { get; set; }

        public QboRefModel CustomerRef { get; set; }
        public QboRefModel ClassRef { get; set; }
        public QboRefModel CustomerMemo { get; set; }

        public QboAddressModel BillAddr { get; set; }
        public QboEmailModel BillEmail { get; set; }

        public List<QboInvoiceLineModel> Line { get; set; }
        public List<QboCustomFieldModel> CustomField { get; set; }
        public QboTxnTaxDetailModel TxnTaxDetail { get; set; }

        public decimal? TotalAmt { get; set; }
        public decimal? Balance { get; set; }
        public decimal? RemainingCredit { get; set; }

        public string PrivateNote { get; set; }

        [JsonProperty("sparse")]
        public bool? Sparse { get; set; }

        public QboMetaDataModel MetaData { get; set; }
    }
}
