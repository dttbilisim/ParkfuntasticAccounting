using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class Building:IEntity<int>{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int StreetId { get; set; }
    public Street Street { get; set; } = null!;
    public ICollection<Home> Homes { get; set; } = new List<Home>();
}
