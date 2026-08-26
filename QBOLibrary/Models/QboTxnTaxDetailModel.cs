namespace QBOLibrary.Models
{
    // Sending TxnTaxCodeRef signals intent to apply Automated Sales Tax.
    // TaxLine is calculated by QuickBooks and is not sent on create.
    public class QboTxnTaxDetailModel
    {
        public QboRefModel TxnTaxCodeRef { get; set; }
        public decimal? TotalTax { get; set; }
    }
}
