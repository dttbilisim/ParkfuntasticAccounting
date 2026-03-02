using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities {
    public class AppSettings : IEntity<int> {
        public int Id { get; set; }
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
        public string Description { get; set; } = null!;
    }
}

