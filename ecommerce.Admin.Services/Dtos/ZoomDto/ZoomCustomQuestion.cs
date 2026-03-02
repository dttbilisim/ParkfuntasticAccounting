using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomCustomQuestion{
    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("value")]
    public string Value { get; set; }
}
