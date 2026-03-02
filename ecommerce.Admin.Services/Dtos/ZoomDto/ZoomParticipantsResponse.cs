using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomParticipantsResponse{
    [JsonProperty("participants")]
    public List<ZoomParticipant> Participants { get; set; }
}
