using System;

namespace QBODataLibrary.Models
{
    public class QboAppDbModel
    {
        public string Environment { get; set; }
        public string Client_id { get; set; }
        public string Client_secret { get; set; }
        public string Redirect_uri { get; set; }
        public bool Use_qbo_backend { get; set; }
        public string Updated_by { get; set; }
        public DateTime Updated_on { get; set; }
    }
}