namespace QBOLibrary.Models
{
    public class QboAccountModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string Name { get; set; }
        public string FullyQualifiedName { get; set; }
        public string AcctNum { get; set; }
        public string Description { get; set; }
        public bool? Active { get; set; }
        public bool? SubAccount { get; set; }

        // Asset, Liability, Equity, Revenue or Expense
        public string Classification { get; set; }
        public string AccountType { get; set; }
        public string AccountSubType { get; set; }

        public decimal? CurrentBalance { get; set; }
        public decimal? CurrentBalanceWithSubAccounts { get; set; }

        public QboRefModel ParentRef { get; set; }
        public QboRefModel CurrencyRef { get; set; }
        public QboMetaDataModel MetaData { get; set; }
    }
}
