using System.Text.Json.Serialization;

namespace Dega.Dtos;

public class CustomOrderRequestDto
{
    public string OrderNote { get; set; }
    public string ReferenceNo { get; set; }
    public List<CustomOrderItemDto> Items { get; set; }
}

public class CustomOrderItemDto
{
    public string ProductCode { get; set; }
    public double Quantity { get; set; }
    public string ItemExp { get; set; }
}

public class CustomOrderResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("message")]
    public DegaResponseMessage MessageDetails { get; set; }

    [JsonPropertyName("d")]
    public DegaOrderData Data { get; set; }

    [JsonIgnore]
    public string OrderNo => Data?.OrderNo;

    [JsonIgnore]
    public string Message => MessageDetails?.Items?.FirstOrDefault()?.Msg;
}

public class DegaResponseMessage
{
    [JsonPropertyName("Items")]
    public List<DegaResponseItem> Items { get; set; }
}

public class DegaResponseItem
{
    public string Kind { get; set; }
    public string Msg { get; set; }
    public string Input { get; set; }
}

public class DegaOrderData
{
    public string OrderNo { get; set; }
    public long OrderId { get; set; }
}
