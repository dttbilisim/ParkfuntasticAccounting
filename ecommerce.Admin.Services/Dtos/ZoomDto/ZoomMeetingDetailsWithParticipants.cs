namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomMeetingDetailsWithParticipants{
    public ZoomMeetingDetailsResponse MeetingDetails { get; set; }
    public List<ZoomParticipantResponse> Participants { get; set; }
}
