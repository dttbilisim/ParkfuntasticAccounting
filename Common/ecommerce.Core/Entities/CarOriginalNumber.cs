using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarOriginalNumber : AuditableEntity<int>
{

    public string Number { get; set; }

    public ICollection<CarSpecOriginalNumber> CarSpecs { get; set; } = new List<CarSpecOriginalNumber>();
}