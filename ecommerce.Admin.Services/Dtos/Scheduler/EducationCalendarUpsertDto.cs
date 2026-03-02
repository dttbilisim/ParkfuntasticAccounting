using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.Scheduler;

[AutoMap(typeof(EducationCalendar), ReverseMap = true)]
public class EducationCalendarUpsertDto{
    public int? Id{get;set;}
    public int CompanyId{get;set;} = 0;
    public UserType UserTypeId{get;set;} = 0;
    public int PharmacyTypeId{get;set;} = 0;
   
    public string Name { get; set; }
    public string ? Description{get;set;}
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Color { get; set; }
    public string Url{get;set;} = "#";
    
    public EntityStatus Status { get; set; }

    [Ignore]
    public bool StatusBool { get; set; } = true;
}
