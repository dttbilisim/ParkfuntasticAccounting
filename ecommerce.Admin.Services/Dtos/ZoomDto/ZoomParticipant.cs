using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomParticipant{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("user_email")]
    public string UserEmail { get; set; }

    [JsonProperty("join_time")]
    public DateTime JoinTime { get; set; }

    [JsonProperty("leave_time")]
    public DateTime LeaveTime { get; set; }

}
