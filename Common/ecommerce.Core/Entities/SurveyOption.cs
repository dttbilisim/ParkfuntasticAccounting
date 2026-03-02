using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class SurveyOption : Entity<int>
{
    public int SurveyId { get; set; }

    public string Title { get; set; } = null!;

    public int Order { get; set; }

    public Survey Survey { get; set; } = null!;
}