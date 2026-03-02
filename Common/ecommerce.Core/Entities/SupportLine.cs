using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities;

public class SupportLine : AuditableEntity<int>
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PhoneNumber { get; set; } = null!;
    public int FrequentlyAskedQuestionsId { get; set; }
    public string Description { get; set; } = null!;
    public string FrequentlyAskedQuestionsName { get; set; }
    public SupportLinereturnType? SupportLineReturnType { get; set; }
    public SupportLineType SupportLineType { get; set; }
    public string? Note { get; set; }
}