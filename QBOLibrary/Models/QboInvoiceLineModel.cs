namespace QBOLibrary.Models
{
    public class QboInvoiceLineModel
    {
        public string Id { get; set; }
        public int? LineNum { get; set; }
        public string Description { get; set; }
        public decimal? Amount { get; set; }

        // SalesItemLineDetail, SubTotalLineDetail, DiscountLineDetail or DescriptionOnly
        public string DetailType { get; set; }

        public QboSalesItemLineDetailModel SalesItemLineDetail { get; set; }
        public QboDescriptionLineDetailModel DescriptionLineDetail { get; set; }
    }
}
