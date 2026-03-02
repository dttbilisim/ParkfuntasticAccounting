using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class SurveyAnswer : Entity<int>
{
    public int SurveyId { get; set; }

    public int SurveyOptionId { get; set; }

    public int CompanyId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(SurveyId))]
    public Survey Survey { get; set; } = null!;

    [ForeignKey(nameof(SurveyOptionId))]
    public SurveyOption SurveyOption { get; set; } = null!;

    [ForeignKey(nameof(CompanyId))]
    public Company Company { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;
}