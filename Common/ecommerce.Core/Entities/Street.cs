using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class Street:IEntity<int>{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int NeighboorId { get; set; }
    public Neighboor Neighboor { get; set; } = null!;
    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
