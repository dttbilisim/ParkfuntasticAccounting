using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities
{
    public class Town : IEntity<int>
    {
        public int Id { get; set; }
        public int CityId { get; set; }
        public string Name { get; set; } = null!;
        public ICollection<Neighboor> Neighboors { get; set; } = new List<Neighboor>();
    }
}

