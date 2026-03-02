namespace ecommerce.Cargo.Mng.Exceptions
{
    public class MngApiException : Exception
    {
        public int? HttpStatusCode { get; set; }

        public string? RequestUrl { get; set; }

        public string? ApiErrorMsg { get; set; }

        public MngApiException()
        {
        }

        public MngApiException(string? message)
            : base(message)
        {
        }

        public MngApiException(string? message, Exception? inner)
            : base(message, inner)
        {
        }
    }
}