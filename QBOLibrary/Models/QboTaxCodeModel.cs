namespace QBOLibrary.Models
{
    // Under Automated Sales Tax the rate lists are managed by QuickBooks
    public class QboTaxCodeModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
        public bool? Taxable { get; set; }
        public bool? TaxGroup { get; set; }
        public QboMetaDataModel MetaData { get; set; }
    }
}
