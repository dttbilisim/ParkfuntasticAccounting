using Newtonsoft.Json;
namespace ecommerce.Admin.Domain.Dtos.ZoomDto;
public class ZoomRegistrant{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("city")]
    public string City { get; set; }

    [JsonProperty("country")]
    public string Country { get; set; }

    [JsonProperty("zip")]
    public string Zip { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }

    [JsonProperty("phone")]
    public string Phone { get; set; }

    [JsonProperty("industry")]
    public string Industry { get; set; }

    [JsonProperty("org")]
    public string Organization { get; set; }

    [JsonProperty("job_title")]
    public string JobTitle { get; set; }

    [JsonProperty("purchasing_time_frame")]
    public string PurchasingTimeFrame { get; set; }

    [JsonProperty("role_in_purchase_process")]
    public string RoleInPurchaseProcess { get; set; }

    [JsonProperty("no_of_employees")]
    public string NumberOfEmployees { get; set; }

    [JsonProperty("comments")]
    public string Comments { get; set; }

    [JsonProperty("join_url")]
    public string JoinUrl { get; set; }

    [JsonProperty("custom_questions")]
    public List<ZoomCustomQuestion> CustomQuestions { get; set; }
}
