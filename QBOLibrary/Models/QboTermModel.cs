namespace QBOLibrary.Models
{
    public class QboTermModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Name { get; set; }
        public bool? Active { get; set; }

        // STANDARD when DueDays is set, DATE_DRIVEN when DayOfMonthDue is set
        public string Type { get; set; }

        public int? DueDays { get; set; }
        public int? DiscountDays { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? DayOfMonthDue { get; set; }
        public int? DiscountDayOfMonth { get; set; }
        public int? DueNextMonthDays { get; set; }

        public QboMetaDataModel MetaData { get; set; }
    }
}
