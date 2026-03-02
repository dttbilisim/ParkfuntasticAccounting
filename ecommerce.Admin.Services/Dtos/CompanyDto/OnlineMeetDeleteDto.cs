using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
public class OnlineMeetDeleteDto{
    public int Id{get;set;}
    public long MeetId{get;set;}

    public int SellerId{get;set;}

    public string SellerName{get;set;}
    public string Subject{get;set;}
    public DateTime MeetDate{get;set;}
    public int Duration{get;set;}
    public string SellerEmail{get;set;}

    public List<OnlineMeetCalendarPharmacy> OnlineMeetCalendarPharmacies{get;set;} = new();
}
