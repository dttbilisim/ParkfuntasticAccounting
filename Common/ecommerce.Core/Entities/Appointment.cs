using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class Appointment: AuditableEntity<int>{

    public int CompanyId{get;set;}
    public string Name { get; set; }
    public string ? Description{get;set;}
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Color { get; set; }
    public int SubjectType{get;set;}
    public string ? Url {get;set;}
    public bool IsZoom{get;set;}
    public bool IsConfirm{get;set;}
    public bool IsCompany{get;set;}
    public long? MeetId{get;set;}
    public string? ApprovedMessage{get;set;}
    
    
    [ForeignKey(nameof(CompanyId))]
    [JsonIgnore]
    public Company Company { get; set; } = null!;
   
}
