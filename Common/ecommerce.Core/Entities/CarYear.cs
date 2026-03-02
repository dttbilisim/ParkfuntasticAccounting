using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarYear : AuditableEntity<int>
{
    public int YearStart { get; set; }
    public int YearEnd { get; set; }
    public string RawText { get; set; }
}