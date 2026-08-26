using System;

namespace QBODataLibrary.Models
{
    public class QboMapDbModel
    {
        public int Id { get; set; }
        public string Environment { get; set; }
        public string Entity { get; set; }
        public string Lims_key { get; set; }
        public string Lims_name { get; set; }
        public string Qbo_id { get; set; }
        public string Qbo_name { get; set; }
        public string Match_status { get; set; }
        public string Updated_by { get; set; }
        public DateTime Updated_on { get; set; }
    }
}