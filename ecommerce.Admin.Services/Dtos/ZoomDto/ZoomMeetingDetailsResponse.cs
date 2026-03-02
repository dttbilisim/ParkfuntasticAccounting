using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomMeetingDetailsResponse{
    [JsonProperty("uuid")] public string Uuid{get;set;}
    [JsonProperty("id")] public string Id{get;set;}
    [JsonProperty("topic")] public string Topic{get;set;}
    [JsonProperty("start_time")] public string StartTime{get;set;}
    [JsonProperty("end_time")] public string EndTime{get;set;} // Toplantının bitiş zamanı
    [JsonProperty("timezone")] public string Timezone{get;set;}
    [JsonProperty("join_url")] public string JoinUrl{get;set;}
    [JsonProperty("status")] public string Status{get;set;} // "waiting", "live", "finished" gibi durumları gösterebilir.
}
