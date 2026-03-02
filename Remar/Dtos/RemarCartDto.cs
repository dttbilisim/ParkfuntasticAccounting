namespace Remar.Dtos;

public class RemarCartItemDto
{
    public string ProductCode { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string? ItemExp { get; set; }
}

public class RemarAddToCartRequestDto
{
    public List<RemarCartItemDto> Items { get; set; } = new();
}

public class RemarAddToCartResponseDto
{
    public RemarOrderResult? d { get; set; }
    public RemarMessage? message { get; set; }
    public string status { get; set; } = string.Empty;
}

public class RemarOrderResult
{
    public string OrderNo { get; set; } = string.Empty;
}

public class RemarMessage
{
    public List<RemarMessageItem> Items { get; set; } = new();
}

public class RemarMessageItem
{
    public string Kind { get; set; } = string.Empty;
    public string Msg { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
}
