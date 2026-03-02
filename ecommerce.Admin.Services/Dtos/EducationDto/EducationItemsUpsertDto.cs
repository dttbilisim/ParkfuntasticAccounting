using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.EducationDto;

[AutoMap(typeof(EducationItems), ReverseMap = true)]
public class EducationItemsUpsertDto{
    public int Id{get;set;}
    public int Status{get;set;}
    public int EducationId{get;set;}
    public string  Name{get;set;}
    public string ? SubText{get;set;}
    public string ? Description{get;set;}
    public int Order {get;set;}
    [Ignore]
    public bool StatusBool { get; set; }

    public string? Url { get; set; }
    public int? Duration { get; set; }
}
