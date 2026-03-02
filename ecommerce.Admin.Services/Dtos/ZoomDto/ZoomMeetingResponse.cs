using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomMeetingResponse{
    public long Id { get; set; }
    public string Topic { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
    [JsonProperty("start_time")]
    public string StartTime { get; set; }

    public int Duration { get; set; }

    [JsonProperty("join_url")]
    public string JoinUrl { get; set; }

    public string Password { get; set; }
}
