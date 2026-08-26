using System;

namespace QBODataLibrary.Models
{
    public class QboLogDbModel
    {
        public long Id { get; set; }
        public string Environment { get; set; }
        public string Entity { get; set; }
        public string Operation { get; set; }
        public string Lims_key { get; set; }
        public string Qbo_id { get; set; }
        public int? Http_status { get; set; }
        public string Intuit_tid { get; set; }
        public string Error_code { get; set; }
        public string Error_msg { get; set; }
        public string Request_json { get; set; }
        public string Response_json { get; set; }
        public string Created_by { get; set; }
        public DateTime Created_on { get; set; }
    }
}