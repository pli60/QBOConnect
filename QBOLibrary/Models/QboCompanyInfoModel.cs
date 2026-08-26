using System.Collections.Generic;
using System.Linq;

namespace QBOLibrary.Models
{
    public class QboCompanyInfoModel
    {
        public string Id { get; set; }
        public string SyncToken { get; set; }
        public string CompanyName { get; set; }
        public string LegalName { get; set; }
        public string Country { get; set; }
        public string SupportedLanguages { get; set; }
        public string DefaultTimeZone { get; set; }
        public string FiscalYearStartMonth { get; set; }
        public string CompanyStartDate { get; set; }

        public QboAddressModel CompanyAddr { get; set; }
        public QboAddressModel LegalAddr { get; set; }
        public QboAddressModel CustomerCommunicationAddr { get; set; }
        public QboEmailModel Email { get; set; }
        public QboEmailModel CustomerCommunicationEmailAddr { get; set; }
        public QboPhoneModel PrimaryPhone { get; set; }

        public List<QboNameValueModel> NameValue { get; set; }
        public QboMetaDataModel MetaData { get; set; }

        // True once a QuickBooks Desktop file has been converted into this company
        public bool IsQbdtMigrated => string.Equals(
            GetNameValue("IsQbdtMigrated"), "true", System.StringComparison.OrdinalIgnoreCase);

        public string GetNameValue(string name)
        {
            return NameValue?.FirstOrDefault(
                n => string.Equals(n.Name, name, System.StringComparison.OrdinalIgnoreCase))?.Value;
        }
    }
}
