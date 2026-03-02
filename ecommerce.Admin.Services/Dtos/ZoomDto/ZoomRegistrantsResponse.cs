using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomRegistrantsResponse{
    [JsonProperty("registrants")]
    public List<ZoomRegistrant> Registrants { get; set; }
}
