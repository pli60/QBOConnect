using System.Collections.Generic;

namespace QBOLibrary.Models
{
    public class QboPaymentLineModel
    {
        public decimal? Amount { get; set; }
        public List<QboLinkedTxnModel> LinkedTxn { get; set; }
    }
}
