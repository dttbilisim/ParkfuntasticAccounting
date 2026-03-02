using ecommerce.Core.Interfaces;
namespace ecommerce.Admin.Domain.Dtos.UserMenuDto
{
    public class UserMenuListDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MenuId { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string? MenuName { get; set; }
        public string? MenuPath { get; set; }
        public string? MenuIcon { get; set; }
    }
}
