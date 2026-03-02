namespace ecommerce.Admin.Domain.Dtos.MenuDto
{
    public class MenuListDto
    {
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string? Tags { get; set; }
        public int Order { get; set; }
        public string? ParentName { get; set; }
        public List<string> RoleNames { get; set; } = new();
    }
}
