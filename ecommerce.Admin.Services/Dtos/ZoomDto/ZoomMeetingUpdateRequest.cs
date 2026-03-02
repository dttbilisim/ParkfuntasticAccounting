namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomMeetingUpdateRequest{
    public string Topic { get; set; }
    public int Type { get; set; } = 2;
    public DateTime StartTime { get; set; } 
    public int Duration { get; set; } 
    public string Timezone { get; set; } = "Europe/Istanbul";
    public string Password { get; set; } = "123456";
}
