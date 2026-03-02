namespace ecommerce.Cargo.Sendeo.Exceptions
{
    public class SendeoApiException : Exception
    {
        public int? HttpStatusCode { get; set; }

        public string? RequestUrl { get; set; }

        public string? ApiErrorMsg { get; set; }

        public SendeoApiException()
        {
        }

        public SendeoApiException(string? message)
            : base(message)
        {
        }

        public SendeoApiException(string? message, Exception? inner)
            : base(message, inner)
        {
        }
    }
}