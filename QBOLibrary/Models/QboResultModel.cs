namespace QBOLibrary.Models
{
    // Every call returns one of these - services never throw for API level failures
    public class QboResultModel<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public int HttpStatus { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetail { get; set; }
        public string IntuitTid { get; set; }
        public string RawResponse { get; set; }

        public string FullError =>
            string.IsNullOrEmpty(ErrorDetail)
                ? ErrorMessage
                : ErrorMessage + " - " + ErrorDetail;

        public static QboResultModel<T> Ok(T data, int httpStatus, string intuitTid, string raw)
        {
            return new QboResultModel<T>
            {
                Success = true,
                Data = data,
                HttpStatus = httpStatus,
                IntuitTid = intuitTid,
                RawResponse = raw
            };
        }

        public static QboResultModel<T> Ok(T data)
        {
            return new QboResultModel<T> { Success = true, Data = data, HttpStatus = 200 };
        }

        public static QboResultModel<T> Fail(int httpStatus, string code, string message,
                                             string detail = null, string intuitTid = null, string raw = null)
        {
            return new QboResultModel<T>
            {
                Success = false,
                HttpStatus = httpStatus,
                ErrorCode = code,
                ErrorMessage = message,
                ErrorDetail = detail,
                IntuitTid = intuitTid,
                RawResponse = raw
            };
        }

        // Carries a failure across a type change without losing the diagnostics
        public static QboResultModel<T> FromFailure<TOther>(QboResultModel<TOther> other)
        {
            return new QboResultModel<T>
            {
                Success = false,
                HttpStatus = other.HttpStatus,
                ErrorCode = other.ErrorCode,
                ErrorMessage = other.ErrorMessage,
                ErrorDetail = other.ErrorDetail,
                IntuitTid = other.IntuitTid,
                RawResponse = other.RawResponse
            };
        }
    }
}
