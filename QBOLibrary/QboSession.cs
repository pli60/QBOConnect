using QBOLibrary.Auth;
using QBOLibrary.Http;
using QBOLibrary.Services;

namespace QBOLibrary
{
    // Single entry point - build one of these and use the service properties
    public class QboSession
    {
        public QboAuth Auth { get; }
        public QboHttp Http { get; }

        public QboCompany Company { get; }
        public QboCustomer Customers { get; }
        public QboCustomerType CustomerTypes { get; }
        public QboInvoice Invoices { get; }
        public QboPayment Payments { get; }
        public QboCreditMemo CreditMemos { get; }
        public QboTerm Terms { get; }
        public QboAccount Accounts { get; }
        public QboItem Items { get; }
        public QboTaxCode TaxCodes { get; }

        public QboSession(IQboTokenStore tokenStore, IQboLogStore logStore)
        {
            Auth = new QboAuth(tokenStore);
            Http = new QboHttp(Auth, logStore);

            Company = new QboCompany(Http);
            Customers = new QboCustomer(Http);
            CustomerTypes = new QboCustomerType(Http);
            Invoices = new QboInvoice(Http);
            Payments = new QboPayment(Http);
            CreditMemos = new QboCreditMemo(Http);
            Terms = new QboTerm(Http);
            Accounts = new QboAccount(Http);
            Items = new QboItem(Http);
            TaxCodes = new QboTaxCode(Http);
        }
    }
}
