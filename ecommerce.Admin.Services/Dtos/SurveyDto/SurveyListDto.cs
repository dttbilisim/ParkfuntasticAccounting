using AutoMapper;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Domain.Dtos.SurveyDto;

[AutoMap(typeof(Survey))]
public class SurveyListDto
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;
    public int? BranchId { get; set; }

    public int Order { get; set; }

    public int SurveyAnswersCount { get; set; }

    public int Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }
}