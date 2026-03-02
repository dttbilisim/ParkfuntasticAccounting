using AutoMapper;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Dtos.SurveyDto;

[AutoMap(typeof(SurveyOptionUpsertDto))]
public class SurveyOptionUpsertDto
{
    public int? Id { get; set; }

    public string Title { get; set; } = null!;

    public int Order { get; set; }
   
}