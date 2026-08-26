namespace QBOLibrary.Http
{
    public class QboLogModel
    {
        public string Environment { get; set; }
        public string Entity { get; set; }
        public string Operation { get; set; }
        public string LimsKey { get; set; }
        public string QboId { get; set; }
        public int? HttpStatus { get; set; }
        public string IntuitTid { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
        public string RequestJson { get; set; }
        public string ResponseJson { get; set; }
        public string CreatedBy { get; set; }
    }
}
