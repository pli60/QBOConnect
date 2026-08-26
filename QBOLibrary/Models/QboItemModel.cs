namespace QBOLibrary.Models
{
    public class QboItemModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Name { get; set; }
        public string FullyQualifiedName { get; set; }
        public string Sku { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }

        // Inventory, Service, NonInventory or Category
        public string Type { get; set; }

        public decimal? UnitPrice { get; set; }
        public bool? Taxable { get; set; }
        public bool? SubItem { get; set; }
        public bool? TrackQtyOnHand { get; set; }
        public decimal? QtyOnHand { get; set; }

        public QboRefModel IncomeAccountRef { get; set; }
        public QboRefModel ExpenseAccountRef { get; set; }
        public QboRefModel AssetAccountRef { get; set; }
        public QboRefModel SalesTaxCodeRef { get; set; }
        public QboRefModel ParentRef { get; set; }
        public QboMetaDataModel MetaData { get; set; }
    }
}
