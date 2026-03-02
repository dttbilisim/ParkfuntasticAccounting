using ecommerce.Core.Interfaces;
namespace ecommerce.Core.Entities;
public class AddressInf:IEntity<int>{
    public int Id { get; set; }
    public string Detail { get; set; } = "";
    public int HomeId { get; set; }
    public Home Home { get; set; } = null!;
}
