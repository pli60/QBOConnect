using Newtonsoft.Json;

namespace QBOLibrary.Models
{
    public class QboCustomerModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }

        // Globally unique across Customer, Vendor, Employee and Other Names
        public string DisplayName { get; set; }
        public string FullyQualifiedName { get; set; }
        public string CompanyName { get; set; }
        public string PrintOnCheckName { get; set; }
        public string GivenName { get; set; }
        public string MiddleName { get; set; }
        public string FamilyName { get; set; }
        public string Title { get; set; }
        public string Suffix { get; set; }

        public bool? Active { get; set; }
        public bool? Job { get; set; }
        public bool? Taxable { get; set; }
        public decimal? Balance { get; set; }
        public decimal? BalanceWithJobs { get; set; }

        public string ResaleNum { get; set; }
        public string Notes { get; set; }
        public string PreferredDeliveryMethod { get; set; }

        public QboEmailModel PrimaryEmailAddr { get; set; }
        public QboPhoneModel PrimaryPhone { get; set; }
        public QboPhoneModel Mobile { get; set; }
        public QboPhoneModel Fax { get; set; }
        public QboAddressModel BillAddr { get; set; }
        public QboAddressModel ShipAddr { get; set; }

        public QboRefModel SalesTermRef { get; set; }
        public QboRefModel CustomerTypeRef { get; set; }
        public QboRefModel DefaultTaxCodeRef { get; set; }
        public QboRefModel ParentRef { get; set; }
        public QboRefModel CurrencyRef { get; set; }

        [JsonProperty("sparse")]
        public bool? Sparse { get; set; }

        public QboMetaDataModel MetaData { get; set; }
    }
}
