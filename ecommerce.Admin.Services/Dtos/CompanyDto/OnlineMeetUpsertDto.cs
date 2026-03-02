using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
[AutoMap(typeof(OnlineMeetCalender),ReverseMap = true)]
public class OnlineMeetUpsertDto{
    public int?  Id{get;set;}
    public int SellerId{get;set;}

  
    public string ? SellerEmail{get;set;}
    public string SellerName{get;set;}
  
    public string Subject{get;set;}
    public DateTime MeetDate{get;set;}
    public int Duration{get;set;} 
    public string ? Description{get;set;}
    public string ? MeetLink{get;set;}
    public bool SellerAproved{get;set;}

    public int CreatedId{get;set;}
    public DateTime CreatedDate{get;set;}
    public long? MeetId{get;set;}
    public string ? Password{get;set;}
    public int? CalenderId{get;set;}

    public int? Type{get;set;}
    public List<OnlineMeetCalendarPharmacy> OnlineMeetCalendarPharmacies{get;set;} = new();
  
    
    public EntityStatus Status { get; set; }
    [Ignore]
    public bool StatusBool { get; set; } = true;
}
