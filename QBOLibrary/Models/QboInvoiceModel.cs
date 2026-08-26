using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using QBOLibrary.Http;

namespace QBOLibrary.Models
{
    public class QboInvoiceModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }

        // Maps to INVOICE.INVOICE_ID - requires custom transaction numbers enabled
        public string DocNumber { get; set; }

        [JsonConverter(typeof(QboDateConverter))]
        public DateTime? TxnDate { get; set; }

        [JsonConverter(typeof(QboDateConverter))]
        public DateTime? DueDate { get; set; }

        public QboRefModel CustomerRef { get; set; }
        public QboRefModel SalesTermRef { get; set; }
        public QboRefModel ClassRef { get; set; }
        public QboRefModel DepartmentRef { get; set; }
        public QboRefModel ProjectRef { get; set; }
        public QboRefModel CustomerMemo { get; set; }

        public QboAddressModel BillAddr { get; set; }
        public QboAddressModel ShipAddr { get; set; }
        public QboAddressModel ShipFromAddr { get; set; }
        public QboEmailModel BillEmail { get; set; }

        public List<QboInvoiceLineModel> Line { get; set; }
        public List<QboLinkedTxnModel> LinkedTxn { get; set; }
        public List<QboCustomFieldModel> CustomField { get; set; }

        public QboTxnTaxDetailModel TxnTaxDetail { get; set; }

        public decimal? TotalAmt { get; set; }
        public decimal? Balance { get; set; }
        public decimal? HomeBalance { get; set; }

        public string PrivateNote { get; set; }
        public string PrintStatus { get; set; }
        public string EmailStatus { get; set; }
        public bool? ApplyTaxAfterDiscount { get; set; }
        public bool? AllowOnlineCreditCardPayment { get; set; }
        public bool? AllowOnlineACHPayment { get; set; }

        [JsonProperty("sparse")]
        public bool? Sparse { get; set; }

        public QboMetaDataModel MetaData { get; set; }
    }
}
