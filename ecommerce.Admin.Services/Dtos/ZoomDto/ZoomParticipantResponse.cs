using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomParticipantResponse{
   
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("user_id")]
    public string UserId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("user_email")]
    public string UserEmail{get;set;}

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("join_time")]
    public DateTime? JoinTime { get; set; }

    [JsonProperty("leave_time")]
    public DateTime? LeaveTime { get; set; }

    [JsonProperty("duration")]
    private int DurationInSeconds { get; set; }
    
    [JsonIgnore]
    public int DurationInMinutes => DurationInSeconds / 60;

    [JsonProperty("attentiveness_score")]
    public string AttentivenessScore { get; set; }

 
}
