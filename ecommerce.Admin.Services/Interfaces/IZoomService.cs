
using ecommerce.Admin.Domain.Dtos;
using ecommerce.Admin.Domain.Dtos.ZoomDto;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IZoomService{
    public Task<IActionResult<string>> GetAccessToken();
    public Task<IActionResult<ZoomMeetingResponse>> CreateMeeting(ZoomCreateRequestDto model);
    public Task<bool> CancelMeetingAsync(long meetingId);
    public Task<bool> UpdateMeetingAsync(long meetingId, ZoomMeetingUpdateRequest updateRequest);
    public Task<ZoomMeetingDetailsResponse> GetMeetingDetailsAsync(long meetingId);
    public Task<List<ZoomParticipantResponse>> GetPastMeetingParticipantsAsync(string meetingUUID);
    public Task<List<ZoomParticipantResponse>> GetMeetingParticipants(long meetingId);
    public Task<List<ZoomParticipantResponse>> GetOngoingMeetingParticipantsAsync(long meetingId);
    public Task<string> AddMeetingRegistrant(long meetingId, string email, string firstName,string lastname);
    public Task<string> AddSurveyToMeeting(long meetingId);
    public Task<ZoomRegistrantsResponse> GetMeetingRegistrants(long meetingId);
    public Task EndMeetingAsync(long meetingId);
    public Task DeleteMeetingAsync(long meetingId);
    public Task<List<ZoomParticipantResponse>> GetMeetingParticipantsReportAsync(long meetingId);
    public Task<List<ZoomRecordingFile>> GetMeetingRecordingsAsync(long meetingId);



}
