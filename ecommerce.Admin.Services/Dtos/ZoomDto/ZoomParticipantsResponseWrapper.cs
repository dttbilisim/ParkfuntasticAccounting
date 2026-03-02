using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomParticipantsResponseWrapper{
    [JsonProperty("page_count")]
    public int PageCount { get; set; }

    [JsonProperty("page_size")]
    public int PageSize { get; set; }

    [JsonProperty("total_records")]
    public int TotalRecords { get; set; }

    [JsonProperty("next_page_token")]
    public string NextPageToken { get; set; }

    [JsonProperty("participants")]
    public List<ZoomParticipantResponse> Participants { get; set; }
}
