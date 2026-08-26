using System;

namespace QBODataLibrary.Models
{
    public class QboTokenDbModel
    {
        public int Id { get; set; }
        public string Environment { get; set; }
        public string Realm_id { get; set; }
        public string Access_token { get; set; }
        public DateTime? Access_expires { get; set; }
        public string Refresh_token { get; set; }
        public DateTime? Refresh_expires { get; set; }
        public bool Refresh_locked { get; set; }
        public string Locked_by { get; set; }
        public DateTime? Locked_at { get; set; }
        public string Updated_by { get; set; }
        public DateTime Updated_on { get; set; }
    }
}