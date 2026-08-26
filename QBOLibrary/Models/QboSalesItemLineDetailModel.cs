using System;

namespace QBOLibrary.Models
{
    public class QboSalesItemLineDetailModel
    {
        public QboRefModel ItemRef { get; set; }
        public QboRefModel ClassRef { get; set; }

        // TAX or NON - omitting this under Automated Sales Tax defaults to TAX
        public QboRefModel TaxCodeRef { get; set; }

        public decimal? UnitPrice { get; set; }
        public decimal? Qty { get; set; }
        public DateTime? ServiceDate { get; set; }
    }
}
