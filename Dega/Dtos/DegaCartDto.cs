namespace Dega.Dtos;

public class DegaCartItemDto
{
    public string ProductCode { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string? ItemExp { get; set; }
}

public class DegaAddToCartRequestDto
{
    public List<DegaCartItemDto> Items { get; set; } = new();
}

public class DegaAddToCartResponseDto
{
    public DegaOrderResult? d { get; set; }
    public DegaMessage? message { get; set; }
    public string status { get; set; } = string.Empty;
}

public class DegaOrderResult
{
    public string OrderNo { get; set; } = string.Empty;
}

public class DegaMessage
{
    public List<DegaMessageItem> Items { get; set; } = new();
}

public class DegaMessageItem
{
    public string Kind { get; set; } = string.Empty;
    public string Msg { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
}
