using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class Home:IEntity<int>{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public ICollection<AddressInf> AddressInfos { get; set; } = new List<AddressInf>();
}
