using System.Net.Http.Headers;
using System.Text;
using ecommerce.Admin.Domain.Dtos.ZoomDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
namespace ecommerce.Admin.Domain.Concreate;
public class ZoomService : IZoomService{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HttpClient _httpClient;
    private readonly string _zoomApiBaseUrl = "https://api.zoom.us/v2";
    public ZoomService(IHttpClientFactory httpClientFactory, HttpClient httpClient){
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClient;
    }
    public async Task<IActionResult<string>> GetAccessToken(){
        IActionResult<string> rs = new(){Result = ""};
        try{
            var clientId = "uuDbwEBOTiWHe8QemsmOQ";
            var clientSecret = "oOebTKwHqUK9sghxvsWcqycmoxiUFjGZ";
            var accountId = "x_GroM_ZQUKksDK1f9XPkw";
            var tokenUrl = $"https://zoom.us/oauth/token?grant_type=account_credentials&account_id={accountId}";
            var client = new RestClient(tokenUrl);
            var request = new RestRequest("", Method.Post);
            request.AddHeader("Authorization", "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            var response = await client.ExecuteAsync(request);
            if(!response.IsSuccessful){
                throw new Exception("Unable to fetch access token: " + response.Content);
            }
            var json = JObject.Parse(response.Content);
            rs.Result = json["access_token"].ToString();
            return rs;
        } catch(Exception e){
            Console.WriteLine(e);
            rs.AddError(e.Message);
            return rs;
        }
        return rs;
    }
    public async Task<IActionResult<ZoomMeetingResponse>> CreateMeeting(ZoomCreateRequestDto model){
        IActionResult<ZoomMeetingResponse> rs = new(){Result = new ZoomMeetingResponse()};
        try{
            var token = await GetAccessToken();
            var client = new HttpClient();
            var localDateTime = DateTime.SpecifyKind(model.OnlineMeetUpsert.MeetDate, DateTimeKind.Unspecified);
            var turkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            var utcStartTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, turkeyTimeZone);
            var start_timeNew = utcStartTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var meetingData = new{
                topic = model.OnlineMeetUpsert.Subject,
                type = 2, // Scheduled meeting
                start_time = start_timeNew,
                duration = model.OnlineMeetUpsert.Duration, // meeting duration in minutes
                timezone = "Europe/Istanbul",
                password = model.OnlineMeetUpsert.Password,
                settings = new{
                    auto_recording = "cloud", // Toplantı otomatik olarak buluta kaydedilecek.
                    join_before_host = true, // Katılımcılar, toplantı sahibi gelmeden önce toplantıya katılabilir.
                    mute_upon_entry = true, // Katılımcılar toplantıya sessiz olarak girecek.
                    registrants_email_notification = false, // Kayıt onaylandığında veya reddedildiğinde e-posta bildirimi kapalı olacak.,
                    waiting_room = false,
                    approval_type = 0,
                    registration_type = 1,
                    meeting_chat = false,
                    auto_saving_chat = false,
                    private_chat = false
                }
            };
            var content = new StringContent(JsonConvert.SerializeObject(meetingData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.zoom.us/v2/users/me/meetings", content);
            if(!response.IsSuccessStatusCode){
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception("Error creating meeting: " + error);
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            var meetingResponse = JsonConvert.DeserializeObject<ZoomMeetingResponse>(responseBody);
            rs.Result = meetingResponse;
            await AddMeetingRegistrant(meetingResponse.Id, model.OnlineMeetUpsert.SellerEmail, model.OnlineMeetUpsert.SellerName, model.OnlineMeetUpsert.SellerName);
            foreach(var item in model.OnlineMeetUpsert.OnlineMeetCalendarPharmacies){
                await AddMeetingRegistrant(meetingResponse.Id, item.Email, item.Name, item.SurName);
            }

            //anket icin servis

            // await AddSurveyToMeeting(meetingResponse.Id);
        } catch(Exception e){
            Console.WriteLine(e);
            rs.AddError(e.Message);
            return rs;
        }
        return rs;
    }
    public async Task<bool> CancelMeetingAsync(long meetingId){
        IActionResult<bool> rs = new(){Result = new bool()};
        try{
            var token = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var response = await _httpClient.DeleteAsync($"https://api.zoom.us/v2/meetings/{meetingId}");
            if(response.IsSuccessStatusCode){
                rs.Result = true;
            } else{
                var error = await response.Content.ReadAsStringAsync();
                rs.Result = false;
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
        return rs.Result;
    }
    public async Task<bool> UpdateMeetingAsync(long meetingId, ZoomMeetingUpdateRequest updateRequest){
        IActionResult<bool> rs = new(){Result = new bool()};
        try{
            var token = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var content = new StringContent(JsonConvert.SerializeObject(updateRequest), Encoding.UTF8, "application/json");
            var response = await _httpClient.PatchAsync($"https://api.zoom.us/v2/meetings/{meetingId}", content);
            if(response.IsSuccessStatusCode){
                rs.Result = true;
            } else{
                var error = await response.Content.ReadAsStringAsync();
                rs.Result = false;
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
        return rs.Result;
    }
    public async Task<ZoomMeetingDetailsResponse> GetMeetingDetailsAsync(long meetingId){
        var token = await GetAccessToken();
        var url = $"{_zoomApiBaseUrl}/meetings/{meetingId}";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
        var response = await _httpClient.GetAsync(url);
        if(!response.IsSuccessStatusCode){
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error retrieving meeting details: {error}");
        }
        var responseBody = await response.Content.ReadAsStringAsync();
        var meetingDetails = JsonConvert.DeserializeObject<ZoomMeetingDetailsResponse>(responseBody);
        return meetingDetails;
    }
    public async Task<List<ZoomParticipantResponse>> GetPastMeetingParticipantsAsync(string meetingUUID){
        try{
            var token = await GetAccessToken();
            var encodedUUID = Uri.EscapeDataString(meetingUUID);
            var url = $"{_zoomApiBaseUrl}/past_meetings/{encodedUUID}/participants";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var response = await _httpClient.GetAsync(url);
            if(!response.IsSuccessStatusCode){
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error retrieving participants: {error}");
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            var participantsResponse = JsonConvert.DeserializeObject<ZoomParticipantsResponseWrapper>(responseBody);
            return participantsResponse?.Participants ?? new List<ZoomParticipantResponse>();
        } catch(Exception e){
            Console.WriteLine($"Exception occurred: {e.Message}");
            throw;
        }
    }
    public async Task<List<ZoomParticipantResponse>> GetMeetingParticipants(long meetingId){
        var meetingDetails = await GetMeetingDetailsAsync(meetingId);
        if(meetingDetails.Status is "waiting" or "live" or "started"){
            return await GetOngoingMeetingParticipantsAsync(meetingId);
        } else{
            return await GetPastMeetingParticipantsAsync(meetingDetails.Uuid);
        }
    }
    public async Task<List<ZoomParticipantResponse>> GetOngoingMeetingParticipantsAsync(long meetingId){
        var token = await GetAccessToken();
        // Doğru endpoint: /metrics/meetings/{meetingId}/participants
        var url = $"{_zoomApiBaseUrl}/metrics/meetings/{meetingId}/participants";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error retrieving participants: {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var participantsResponse = JsonConvert.DeserializeObject<ZoomParticipantsResponseWrapper>(responseBody);
        return participantsResponse.Participants;
    }
    public async Task<string> AddMeetingRegistrant(long meetingId, string email, string firstName, string lastname){
        try{
            var token = await GetAccessToken();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var registrantData = new{email = email, first_name = firstName, last_name = lastname};
            var content = new StringContent(JsonConvert.SerializeObject(registrantData), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://api.zoom.us/v2/meetings/{meetingId}/registrants", content);
            if(!response.IsSuccessStatusCode){
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception("Error adding registrant: " + error);
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody;
        } catch(Exception e){
            Console.WriteLine(e);
            return e.Message;
        }
    }
    public async Task<string> AddSurveyToMeeting(long meetingId){
        try{
            var token = await GetAccessToken();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var surveyData = new{title = "Toplantı Sonrası Anket", questions = new[]{new{question = "Toplantıdan memnun kaldınız mı?", type = "single", answers = new[]{"Evet", "Hayır"}}, new{question = "Parpazar Alışveriş Yaptınızmı?", type = "single", answers = new[]{"Evet", "Hayır"}}, new{question = "Bu tarz Toplantıların Devam Etmesini İstiyormusunuz?", type = "single", answers = new[]{"Evet", "Hayır"}}}};
            var content = new StringContent(JsonConvert.SerializeObject(surveyData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"https://api.zoom.us/v2/meetings/{meetingId}/surveys", content);
            if(!response.IsSuccessStatusCode){
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error adding survey to meeting: {error}");
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            return responseBody; // Başarılı olursa yanıtı döndür
        } catch(Exception ex){
            Console.WriteLine($"Hata: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }
    public async Task<ZoomRegistrantsResponse> GetMeetingRegistrants(long meetingId){
        try{
            var token = await GetAccessToken();
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
            var response = await client.GetAsync($"https://api.zoom.us/v2/meetings/{meetingId}/registrants");
            if(!response.IsSuccessStatusCode){
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception("Error retrieving registrants: " + error);
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ZoomRegistrantsResponse>(responseBody);
            return result;
        } catch(Exception e){
            Console.WriteLine(e.Message);
            throw;
        }
    }
    public async Task EndMeetingAsync(long meetingId){

      
        var token = await GetAccessToken();
        var url = $"{_zoomApiBaseUrl}/meetings/{meetingId}/status";
        var data = new{action = "end"};
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);
        var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(url, content);
        if(!response.IsSuccessStatusCode){
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error ending meeting: {error}");
        }
    }
    public async Task DeleteMeetingAsync(long meetingId){
        var token = await GetAccessToken();
        var url = $"{_zoomApiBaseUrl}/meetings/{meetingId}";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);

        var response = await _httpClient.DeleteAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error deleting meeting: {error}");
        }
    }
    public async Task<List<ZoomParticipantResponse>> GetMeetingParticipantsReportAsync(long meetingId){
        var token = await GetAccessToken();
        var url = $"{_zoomApiBaseUrl}/report/meetings/{meetingId}/participants";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error retrieving participants report: {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var participantsResponse = JsonConvert.DeserializeObject<ZoomParticipantsResponseWrapper>(responseBody);
        return participantsResponse.Participants;
    }
    public async Task<List<ZoomRecordingFile>> GetMeetingRecordingsAsync(long meetingId){
        var token = await GetAccessToken();
        var url = $"{_zoomApiBaseUrl}/meetings/{meetingId}/recordings";
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Result);

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Error retrieving recordings: {error}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();
        var recordingsResponse = JsonConvert.DeserializeObject<ZoomRecordingResponse>(responseBody);
        return recordingsResponse.RecordingFiles;
    }
    public string ConvertToUtc(DateTime localDateTime){
        var istanbulTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, istanbulTimeZone);
        return utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}
