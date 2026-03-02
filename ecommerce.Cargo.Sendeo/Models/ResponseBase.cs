namespace ecommerce.Cargo.Sendeo.Models;

public class ResponseBase
{
    public string? RequestId { get; set; }

    public string? ExceptionMessage { get; set; }

    public string? InnerExceptionMessage { get; set; }

    public string? ExceptionDescription { get; set; }

    public int StatusCode { get; set; }
}

public class ResponseBase<T> : ResponseBase
{
    public T Result { get; set; } = default!;
}