using AutoMapper;
using AutoMapper.Configuration.Annotations;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Domain.Dtos.EmailDto;

[AutoMap(typeof(EmailTemplates), ReverseMap = true)]
public class EmailTemplatesDto{
    public int? Id { get; set; }
    public string Name{get;set;}
    public int Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public EmailTemplateType EmailTemplateType{get;set;}
    /// <summary>Şube (tenant) - liste ve CRUD bu değere göre filtrelenir. Null = seçilmemiş / ortak.</summary>
    public int? BranchId { get; set; }
    [Ignore]
    public bool StatusBool { get; set; } = true;
}
