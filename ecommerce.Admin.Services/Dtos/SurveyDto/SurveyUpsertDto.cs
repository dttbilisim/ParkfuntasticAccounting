using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.SurveyDto;

[AutoMap(typeof(Survey), ReverseMap = true)]
public class SurveyUpsertDto
{
    public int? Id { get; set; }

    public string Title { get; set; } = null!;
    public int? BranchId { get; set; }

    public string? Description { get; set; }

    public int Order { get; set; }

    public int Status { get; set; }

    public List<SurveyOptionUpsertDto> SurveyOptions { get; set; } = new();
   
}