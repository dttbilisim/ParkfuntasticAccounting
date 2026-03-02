using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.SurveyDto;

[AutoMap(typeof(SurveyAnswer))]
public class SurveyAnswerListDto
{
    public int Id { get; set; }

    public int SurveyOptionId { get; set; }

    public int CompanyId { get; set; }

    public int UserId { get; set; }

    public string SurveyOptionTitle { get; set; } = null!;

    public string? CompanyAccountName { get; set; }

    public string? UserFirstName { get; set; }

    public string? UserLastName { get; set; }
}