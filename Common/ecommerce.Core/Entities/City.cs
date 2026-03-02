using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities {
    public class City:IEntity<int>
	{
        public int Id { get; set; } 
        public string Name { get; set; } = null!;
    }
}

