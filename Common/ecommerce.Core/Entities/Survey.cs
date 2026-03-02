using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class Survey : AuditableEntity<int>
{
    public int? BranchId { get; set; }
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public int Order { get; set; }

    public ICollection<SurveyOption> SurveyOptions { get; set; } = new List<SurveyOption>();

    public ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();
}