using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Admin {
    public class Menu : IEntity<int> {
        public int Id {get;set;}
        public int? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Icon { get;set; } = null!;
        public string? Tags { get; set; }
        
        public int Order { get; set; }
        public virtual Menu? Parent { get; set; }
        public virtual ICollection<Menu> InverseParent { get; set; } = new List<Menu>();
        public virtual ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
        public virtual ICollection<UserMenu> UserMenus { get; set; } = new List<UserMenu>();
    }
}
