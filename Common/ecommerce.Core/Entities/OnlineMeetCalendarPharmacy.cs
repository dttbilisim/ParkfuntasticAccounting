using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class OnlineMeetCalendarPharmacy:IEntity<int>{
   
    public int Id{get;set;}
    public int OnlineMeetId {get;set;}
    public int CompanyId{get;set;}
    public string CompanyName{get;set;}
    public string Name{get;set;}
    public string SurName{get;set;}
    public string Email{get;set;}
    public bool IsApproved{get;set;}
    public string? ApprovedMessage{get;set;}
    
    
    
    [ForeignKey("OnlineMeetId")] public OnlineMeetCalender OnlineMeetCalender{get;set;}
}
