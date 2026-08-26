using QBODataLibrary;
using QBODataLibrary.Db;
using QBOLibrary;
using QBOLibrary.Auth;
using QBOLibrary.Models;
using QBOLibrary.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace QBOTestConsole
{
    class Program
    {
        // ---- fill these in before first run -------------------------------
        private const string ConnectionString =
            "Data Source=10.1.10.225;Initial Catalog=lims_dev;User ID=limsdevuser;Password=groundPork3$";
        private const string ClientId = "ABl3XbUZHmzwxYrvUvTZCTv8FsaBwOIg7AdtYkMkh4iU3iK896";
        private const string ClientSecret = "hFLz8KpSIRK1LfesewWp0tgsnZSwz8vCVoqMIgRY";
        private const string RedirectUri = "https://developer.intuit.com/v2/OAuth2Playground/RedirectUrl";
        // -------------------------------------------------------------------

        private static QboSession _session;
        private static QboTokenAccess _tokenAccess;
        private static QboLogAccess _logAccess;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            ChangeConn.ChangeConnStr(ConnectionString);

            QboConfig.IsDev = true;
            QboConfig.ClientId = ClientId;
            QboConfig.ClientSecret = ClientSecret;
            QboConfig.RedirectUri = RedirectUri;

            _tokenAccess = new QboTokenAccess();
            _logAccess = new QboLogAccess();
            _session = new QboSession(_tokenAccess, _logAccess);

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=== QBOConnect harness [" + QboConfig.Environment + "] ===");
                Console.WriteLine(" 1. Authorize (one time - opens OAuth flow)");
                Console.WriteLine(" 2. Company info");
                Console.WriteLine(" 3. Force token refresh");
                Console.WriteLine(" 4. Reference data (Term / Account / Item / TaxCode / CustomerType)");
                Console.WriteLine(" 5. Customers - read");
                Console.WriteLine(" 6. Customer - create / modify / deactivate");
                Console.WriteLine(" 7. Invoice - create / modify / void");
                Console.WriteLine(" 8. Payment - create against an invoice");
                Console.WriteLine(" 9. Credit memo - create");
                Console.WriteLine("10. Show recent API log");
                Console.WriteLine("11. Zero-dollar line tests");
                Console.WriteLine(" 0. Exit");
                Console.Write("> ");

                string choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1": await AuthorizeAsync(); break;
                        case "2": await CompanyAsync(); break;
                        case "3": await ForceRefreshAsync(); break;
                        case "4": await ReferenceDataAsync(); break;
                        case "5": await CustomersReadAsync(); break;
                        case "6": await CustomerWriteAsync(); break;
                        case "7": await InvoiceWriteAsync(); break;
                        case "8": await PaymentWriteAsync(); break;
                        case "9": await CreditMemoWriteAsync(); break;
                        case "10": ShowLog(); break;
                        case "11": await ZeroDollarTestsAsync(); break;
                        case "0": return;
                        default: Console.WriteLine("Unknown option."); break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("UNHANDLED: " + ex);
                }
            }
        }

        // 1 -----------------------------------------------------------------
        static async Task AuthorizeAsync()
        {
            string url = QboAuth.BuildAuthorizeUrl("limstest");

            Console.WriteLine("Open this URL, approve the sandbox company, then copy the full");
            Console.WriteLine("address bar contents of the page you land on:");
            Console.WriteLine();
            Console.WriteLine(url);
            Console.WriteLine();
            Console.Write("Paste redirect URL: ");

            string redirect = Console.ReadLine();
            string code = ReadQueryValue(redirect, "code");
            string realmId = ReadQueryValue(redirect, "realmId");

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId))
            {
                Console.WriteLine("Could not find code and realmId in that URL.");
                return;
            }

            Console.WriteLine("code=" + Trim(code) + "  realmId=" + realmId);

            QboResultModel<QboTokenModel> result =
                await _session.Auth.ExchangeCodeAsync(code, realmId);

            if (!result.Success)
            {
                Console.WriteLine("FAILED " + result.ErrorCode + ": " + result.FullError);
                return;
            }

            Console.WriteLine("Token exchange succeeded.");
            Console.WriteLine("  access expires : " + result.Data.AccessExpires);
            Console.WriteLine("  refresh expires: " + result.Data.RefreshExpires);

            // Read back to confirm the database write actually landed
            QboTokenModel saved = _tokenAccess.Get();
            if (saved == null)
            {
                Console.WriteLine("  DB WRITE FAILED - no row returned from QBO_TOKEN.");
                return;
            }
            Console.WriteLine("  saved to DB    : " + (saved.AccessToken == result.Data.AccessToken
                                                       ? "OK" : "MISMATCH - write did not land"));
            Console.WriteLine("  token length   : " + (saved.AccessToken?.Length ?? 0));
        }

        // 2 -----------------------------------------------------------------
        static async Task CompanyAsync()
        {
            QboResultModel<QboCompanyInfoModel> result = await _session.Company.GetAsync();
            if (!Ok(result)) return;

            QboCompanyInfoModel info = result.Data;
            Console.WriteLine("Company      : " + info.CompanyName);
            Console.WriteLine("Legal name   : " + info.LegalName);
            Console.WriteLine("Country      : " + info.Country);
            Console.WriteLine("Time zone    : " + info.DefaultTimeZone);
            Console.WriteLine("Qbdt migrated: " + info.IsQbdtMigrated);
            Console.WriteLine("intuit_tid   : " + result.IntuitTid);
        }

        // 3 -----------------------------------------------------------------
        static async Task ForceRefreshAsync()
        {
            QboTokenModel before = _tokenAccess.Get();
            if (before == null)
            {
                Console.WriteLine("No token row. Run option 1 first.");
                return;
            }

            Console.WriteLine("before access token : " + Trim(before.AccessToken));
            Console.WriteLine("before refresh token: " + Trim(before.RefreshToken));

            // Expire the access token so the next call has to refresh
            new SqlDb().Save<dynamic>(
                @"update QBO_TOKEN set ACCESS_EXPIRES = DATEADD(minute, -10, GETDATE())
                  where ENVIRONMENT = @environment; select 1;",
                new { environment = QboConfig.Environment },
                CommandType.Text);

            Console.WriteLine("Access token expired. Calling CompanyInfo...");

            QboResultModel<QboCompanyInfoModel> result = await _session.Company.GetAsync();
            if (!Ok(result)) return;

            QboTokenModel after = _tokenAccess.Get();

            Console.WriteLine("after  access token : " + Trim(after.AccessToken));
            Console.WriteLine("after  refresh token: " + Trim(after.RefreshToken));
            Console.WriteLine();
            Console.WriteLine("access token changed : " + (before.AccessToken != after.AccessToken)
                              + "   <- must be True");
            Console.WriteLine("expiry moved forward : " + (after.AccessExpires > DateTime.Now)
                              + "   <- must be True");
            Console.WriteLine("refresh token rotated: " + (before.RefreshToken != after.RefreshToken)
                              + "   (rotates ~daily, False is normal)");
            Console.WriteLine("lock released        : " + (after.RefreshLocked == false));
        }

        // 4 -----------------------------------------------------------------
        static async Task ReferenceDataAsync()
        {
            var terms = await _session.Terms.QueryAllAsync();
            if (Ok(terms))
            {
                Console.WriteLine("Terms (" + terms.Data.Count + "):");
                foreach (QboTermModel t in terms.Data.Take(10))
                {
                    Console.WriteLine("  " + t.Id.PadLeft(4) + "  " + t.Name + "  DueDays=" + t.DueDays);
                }
            }

            var accounts = await _session.Accounts.QueryActiveAsync();
            if (Ok(accounts))
            {
                Console.WriteLine("Accounts (" + accounts.Data.Count + "), first 10:");
                foreach (QboAccountModel a in accounts.Data.Take(10))
                {
                    Console.WriteLine("  " + a.Id.PadLeft(4) + "  " + a.Name + "  [" + a.AccountType + "]");
                }
            }

            var items = await _session.Items.QueryActiveAsync();
            if (Ok(items))
            {
                Console.WriteLine("Items (" + items.Data.Count + "), first 10:");
                foreach (QboItemModel i in items.Data.Take(10))
                {
                    Console.WriteLine("  " + i.Id.PadLeft(4) + "  " + i.Name +
                                      "  " + i.Type + "  " + i.UnitPrice);
                }
            }

            var taxCodes = await _session.TaxCodes.QueryAllAsync();
            if (Ok(taxCodes))
            {
                Console.WriteLine("TaxCodes (" + taxCodes.Data.Count + "):");
                foreach (QboTaxCodeModel tc in taxCodes.Data.Take(10))
                {
                    Console.WriteLine("  " + tc.Id.PadLeft(4) + "  " + tc.Name + "  taxable=" + tc.Taxable);
                }
            }

            var types = await _session.CustomerTypes.QueryAllAsync();
            if (Ok(types))
            {
                Console.WriteLine("CustomerTypes (" + types.Data.Count + ")");
                foreach (QboCustomerTypeModel ct in types.Data.Take(10))
                {
                    Console.WriteLine("  " + ct.Id + "  " + ct.Name);
                }
            }
        }

        // 5 -----------------------------------------------------------------
        static async Task CustomersReadAsync()
        {
            var all = await _session.Customers.QueryActiveAsync();
            if (!Ok(all)) return;

            Console.WriteLine("Active customers: " + all.Data.Count);
            foreach (QboCustomerModel c in all.Data.Take(15))
            {
                Console.WriteLine("  " + c.Id.PadLeft(4) + "  " + c.DisplayName +
                                  "  balance=" + c.Balance);
            }

            var withBalance = await _session.Customers.QueryWithBalanceAsync();
            if (Ok(withBalance))
            {
                Console.WriteLine("With outstanding balance: " + withBalance.Data.Count);
            }
        }

        // 6 -----------------------------------------------------------------
        static async Task CustomerWriteAsync()
        {
            string name = "LIMS Test " + DateTime.Now.ToString("MMdd-HHmmss");

            var created = await _session.Customers.AddAsync(new QboCustomerModel
            {
                DisplayName = name,
                CompanyName = name,
                PrimaryEmailAddr = new QboEmailModel("peter@aemtek.com"),
                PrimaryPhone = new QboPhoneModel("831-555-0100"),
                BillAddr = new QboAddressModel
                {
                    Line1 = "123 Sierra Way",
                    City = "San Pablo",
                    CountrySubDivisionCode = "CA",
                    PostalCode = "87999"
                }
            });
            if (!Ok(created)) return;
            Console.WriteLine("Created Id=" + created.Data.Id + "  SyncToken=" + created.Data.SyncToken);

            // Duplicate DisplayName must fail with 6240
            var duplicate = await _session.Customers.AddAsync(new QboCustomerModel { DisplayName = name });
            Console.WriteLine("Duplicate attempt -> " +
                (duplicate.Success ? "UNEXPECTED SUCCESS" : duplicate.ErrorCode + ": " + duplicate.ErrorMessage));

            // Modify - SyncToken is handled inside the library
            created.Data.CompanyName = name + " (edited)";
            var modified = await _session.Customers.ModifyAsync(created.Data);
            if (Ok(modified))
            {
                Console.WriteLine("Modified. SyncToken now " + modified.Data.SyncToken);
            }

            // IListDel has no equivalent - deactivate instead
            var deactivated = await _session.Customers.DeactivateAsync(created.Data.Id);
            if (Ok(deactivated))
            {
                Console.WriteLine("Deactivated. Active=" + deactivated.Data.Active);
            }
        }

        // 7 -----------------------------------------------------------------
        static async Task InvoiceWriteAsync()
        {
            var customers = await _session.Customers.QueryActiveAsync();
            if (!Ok(customers) || customers.Data.Count == 0) return;

            var items = await _session.Items.QueryActiveAsync();
            if (!Ok(items)) return;

            QboCustomerModel customer = customers.Data.First();
            QboItemModel item = items.Data.FirstOrDefault(i => i.Type == "Service") ?? items.Data.First();

            string docNumber = "LIMS" + DateTime.Now.ToString("MMddHHmmss");
            Console.WriteLine("Customer=" + customer.DisplayName + "  Item=" + item.Name);

            var created = await _session.Invoices.AddAsync(new QboInvoiceModel
            {
                DocNumber = docNumber,
                TxnDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(30),
                CustomerRef = new QboRefModel(customer.Id, customer.DisplayName),
                CustomerMemo = new QboRefModel { Value = "Created by QBOConnect harness" },
                Line = new List<QboInvoiceLineModel>
                {
                    new QboInvoiceLineModel
                    {
                        DetailType = "SalesItemLineDetail",
                        Description = "Total Coliform - Water",
                        Amount = 125.00m,
                        SalesItemLineDetail = new QboSalesItemLineDetailModel
                        {
                            ItemRef = new QboRefModel(item.Id, item.Name),
                            Qty = 1,
                            UnitPrice = 125.00m,
                            TaxCodeRef = new QboRefModel("NON")
                        }
                    }
                }
            });
            if (!Ok(created)) return;

            Console.WriteLine("Created Id=" + created.Data.Id +
                              "  DocNumber=" + created.Data.DocNumber +
                              "  Total=" + created.Data.TotalAmt +
                              "  Balance=" + created.Data.Balance);

            // The duplicate guard should refuse a second post of the same DocNumber
            var again = await _session.Invoices.AddAsync(new QboInvoiceModel
            {
                DocNumber = docNumber,
                CustomerRef = new QboRefModel(customer.Id),
                Line = new List<QboInvoiceLineModel>
                {
                    new QboInvoiceLineModel
                    {
                        DetailType = "SalesItemLineDetail",
                        Amount = 1.00m,
                        SalesItemLineDetail = new QboSalesItemLineDetailModel
                        {
                            ItemRef = new QboRefModel(item.Id)
                        }
                    }
                }
            });
            Console.WriteLine("Duplicate DocNumber -> " +
                (again.Success ? "UNEXPECTED SUCCESS" : again.ErrorCode + ": " + again.ErrorMessage));

            // Sparse update - only PrivateNote changes
            var patched = await _session.Invoices.ModifySparseAsync(new QboInvoiceModel
            {
                Id = created.Data.Id,
                PrivateNote = "Patched " + DateTime.Now.ToString("HH:mm:ss")
            });
            if (Ok(patched))
            {
                Console.WriteLine("Sparse update ok. Total still " + patched.Data.TotalAmt);
            }

            Console.Write("Void this invoice? (y/n) ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                var voided = await _session.Invoices.VoidAsync(created.Data.Id);
                if (Ok(voided))
                {
                    Console.WriteLine("Voided. Total=" + voided.Data.TotalAmt +
                                      "  Note=" + voided.Data.PrivateNote);
                }
            }
            else
            {
                Console.WriteLine("Keep this Id for option 8: " + created.Data.Id);
            }
        }

        // 8 -----------------------------------------------------------------
        static async Task PaymentWriteAsync()
        {
            var unpaid = await _session.Invoices.QueryUnpaidAsync();
            if (!Ok(unpaid)) return;
            if (unpaid.Data.Count == 0)
            {
                Console.WriteLine("No unpaid invoices. Run option 7 first and skip the void.");
                return;
            }

            QboInvoiceModel invoice = unpaid.Data.First();
            Console.WriteLine("Paying invoice " + invoice.Id +
                              " (" + invoice.DocNumber + ") balance " + invoice.Balance);

            QboPaymentModel payment = QboPayment.BuildApplied(
                invoice.CustomerRef.Value,
                invoice.Balance ?? 0m,
                DateTime.Today,
                "CHK" + DateTime.Now.ToString("HHmmss"),
                new Dictionary<string, decimal> { { invoice.Id, invoice.Balance ?? 0m } });

            var created = await _session.Payments.AddAsync(payment);
            if (!Ok(created)) return;

            Console.WriteLine("Payment Id=" + created.Data.Id + "  Total=" + created.Data.TotalAmt);

            var recheck = await _session.Invoices.GetByIdAsync(invoice.Id);
            if (Ok(recheck))
            {
                Console.WriteLine("Invoice balance now " + recheck.Data.Balance +
                                  "  LinkedTxn=" + (recheck.Data.LinkedTxn?.Count ?? 0));
            }
        }

        // 9 -----------------------------------------------------------------
        static async Task CreditMemoWriteAsync()
        {
            var customers = await _session.Customers.QueryActiveAsync();
            if (!Ok(customers) || customers.Data.Count == 0) return;

            var items = await _session.Items.QueryActiveAsync();
            if (!Ok(items) || items.Data.Count == 0) return;

            QboCustomerModel customer = customers.Data.First();
            QboItemModel item = items.Data.FirstOrDefault(i => i.Type == "Service")
                    ?? items.Data.FirstOrDefault(i => i.Type != "Category");
            if (item == null)
            {
                Console.WriteLine("No billable item found - all items are categories.");
                return;
            }

            var created = await _session.CreditMemos.AddAsync(new QboCreditMemoModel
            {
                TxnDate = DateTime.Today,
                CustomerRef = new QboRefModel(customer.Id, customer.DisplayName),
                Line = new List<QboInvoiceLineModel>
                {
                    new QboInvoiceLineModel
                    {
                        DetailType = "SalesItemLineDetail",
                        Description = "Re-test credit",
                        Amount = 25.00m,
                        SalesItemLineDetail = new QboSalesItemLineDetailModel
                        {
                            ItemRef = new QboRefModel(item.Id, item.Name),
                            Qty = 1,
                            UnitPrice = 25.00m,
                            TaxCodeRef = new QboRefModel("NON")
                        }
                    }
                }
            });
            if (!Ok(created)) return;

            Console.WriteLine("Credit memo Id=" + created.Data.Id +
                              "  Total=" + created.Data.TotalAmt +
                              "  RemainingCredit=" + created.Data.RemainingCredit);
        }

        // 10 ----------------------------------------------------------------
        static void ShowLog()
        {
            var rows = _logAccess.GetRecent(20);
            Console.WriteLine("Recent API calls:");
            foreach (var r in rows)
            {
                Console.WriteLine("  " + r.Created_on.ToString("HH:mm:ss") +
                                  "  " + (r.Entity ?? "").PadRight(14) +
                                  (r.Operation ?? "").PadRight(8) +
                                  "HTTP " + r.Http_status +
                                  (string.IsNullOrEmpty(r.Error_code) ? "" : "  [" + r.Error_code + "] " + r.Error_msg));
            }
        }

        // 11 ----------------------------------------------------------------
        static async Task ZeroDollarTestsAsync()
        {
            var customers = await _session.Customers.QueryActiveAsync();
            if (!Ok(customers) || customers.Data.Count == 0) return;

            var items = await _session.Items.QueryActiveAsync();
            if (!Ok(items)) return;

            QboCustomerModel customer = customers.Data.First();
            QboItemModel item = items.Data.FirstOrDefault(i => i.Type == "Service")
                                ?? items.Data.FirstOrDefault(i => i.Type != "Category");
            if (item == null)
            {
                Console.WriteLine("No billable item found.");
                return;
            }

            Console.WriteLine("Customer=" + customer.DisplayName + "  Item=" + item.Name);
            Console.WriteLine();

            // A - zero amount on a normal sales line (the SyncHelpers pattern)
            await TryInvoiceAsync("A: zero-amount sales line", customer, new List<QboInvoiceLineModel>
            {
                BuildSalesLine(item, "No charge - re-test", 0m, 0m)
            });

            // B - DescriptionOnly line alongside a billable line
            await TryInvoiceAsync("B: DescriptionOnly + billable line", customer, new List<QboInvoiceLineModel>
            {
                new QboInvoiceLineModel
                {
                    DetailType = "DescriptionOnly",
                    Description = "Sample collected 8/24/2026 by field tech",
                    DescriptionLineDetail = new QboDescriptionLineDetailModel()
                },
                BuildSalesLine(item, "Total Coliform - Water", 125.00m, 125.00m)
            });

            // C - every line zero, invoice totals zero
            await TryInvoiceAsync("C: all lines zero", customer, new List<QboInvoiceLineModel>
            {
                BuildSalesLine(item, "Courtesy re-test 1", 0m, 0m),
                BuildSalesLine(item, "Courtesy re-test 2", 0m, 0m)
            });
        }

        static QboInvoiceLineModel BuildSalesLine(QboItemModel item, string description,
                                                  decimal amount, decimal unitPrice)
        {
            return new QboInvoiceLineModel
            {
                DetailType = "SalesItemLineDetail",
                Description = description,
                Amount = amount,
                SalesItemLineDetail = new QboSalesItemLineDetailModel
                {
                    ItemRef = new QboRefModel(item.Id, item.Name),
                    Qty = 1,
                    UnitPrice = unitPrice,
                    TaxCodeRef = new QboRefModel("NON")
                }
            };
        }

        static async Task TryInvoiceAsync(string label, QboCustomerModel customer,
                                          List<QboInvoiceLineModel> lines)
        {
            var result = await _session.Invoices.AddAsync(new QboInvoiceModel
            {
                DocNumber = "ZT" + DateTime.Now.ToString("MMddHHmmssfff"),
                TxnDate = DateTime.Today,
                CustomerRef = new QboRefModel(customer.Id, customer.DisplayName),
                Line = lines
            });

            if (result.Success)
            {
                Console.WriteLine(label);
                Console.WriteLine("   ACCEPTED  Id=" + result.Data.Id +
                                  "  Total=" + result.Data.TotalAmt +
                                  "  Lines=" + (result.Data.Line?.Count ?? 0));
            }
            else
            {
                Console.WriteLine(label);
                Console.WriteLine("   REJECTED  " + result.ErrorCode + ": " + result.ErrorMessage);
                if (!string.IsNullOrEmpty(result.ErrorDetail))
                {
                    Console.WriteLine("             " + result.ErrorDetail);
                }
            }
            Console.WriteLine();
        }

        // helpers -----------------------------------------------------------
        static bool Ok<T>(QboResultModel<T> result)
        {
            if (result.Success)
            {
                return true;
            }
            Console.WriteLine("FAILED HTTP " + result.HttpStatus +
                              "  " + result.ErrorCode + ": " + result.FullError +
                              (string.IsNullOrEmpty(result.IntuitTid) ? "" : "  tid=" + result.IntuitTid));
            return false;
        }

        static string ReadQueryValue(string url, string key)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }
            int q = url.IndexOf('?');
            string query = q >= 0 ? url.Substring(q + 1) : url;

            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }
                if (string.Equals(pair.Substring(0, eq), key, StringComparison.OrdinalIgnoreCase))
                {
                    return Uri.UnescapeDataString(pair.Substring(eq + 1));
                }
            }
            return null;
        }

        static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(null)";
            }
            return value.Length <= 16 ? value : value.Substring(0, 16) + "...";
        }
    }
}