using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.CompanyDto;

[AutoMap(typeof(CompanyDocument))]
public class CompanyDocumentListDto{
    public int  Id{get;set;}
    public string Email{get;set;}
    public int FileId{get;set;}
    public string FileName{get;set;}
    public string Base64data{get;set;}
    public string Root{get;set;}
    public string ContentType { get; set; }
}
