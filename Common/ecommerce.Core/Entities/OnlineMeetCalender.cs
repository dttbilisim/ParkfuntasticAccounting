using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class OnlineMeetCalender: AuditableEntity<int>{
    public int  Id{get;set;}
    public int SellerId{get;set;}
    public string SellerName{get;set;}
    public string Subject{get;set;}
    public DateTime MeetDate{get;set;}
    public int Duration{get;set;} 
    public string ? Description{get;set;}
    public string ? MeetLink{get;set;}
    public long? MeetId{get;set;}
    public string ? Password{get;set;}
    public string ? SellerEmail{get;set;}
    public int? CalenderId{get;set;}
    public List<OnlineMeetCalendarPharmacy> OnlineMeetCalendarPharmacies{get;set;} = new();

    [ForeignKey("SellerId")] public Company Seller{get;set;}
    
   
}
