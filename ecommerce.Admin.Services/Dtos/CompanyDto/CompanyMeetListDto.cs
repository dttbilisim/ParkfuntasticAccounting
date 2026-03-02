using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;
[AutoMap(typeof(CompanyMeet))]
public class CompanyMeetListDto{
    public int Id{get;set;}
    public int CompanyId{get;set;}
    public DateTime MeetDate{get;set;}
    public int MeetCount{get;set;}
    public int MeetCounUse{get;set;}
    
    public Company Company { get; set; }
}
