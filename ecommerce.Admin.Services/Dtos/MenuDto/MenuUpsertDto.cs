namespace ecommerce.Admin.Domain.Dtos.MenuDto
{
    public class MenuUpsertDto
    {
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Icon { get; set; } = null!;
        public string? Tags { get; set; }
        public List<int> SelectedRoleIds { get; set; } = new();
    }
}
