using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class Neighboor:IEntity<int>{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TownId { get; set; }
    public Town Town { get; set; } = null!;
    public ICollection<Street> Streets { get; set; } = new List<Street>();
}
