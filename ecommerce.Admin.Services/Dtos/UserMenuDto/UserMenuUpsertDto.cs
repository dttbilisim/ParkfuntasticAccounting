using ecommerce.Core.Interfaces;
namespace ecommerce.Admin.Domain.Dtos.UserMenuDto
{
    public class UserMenuUpsertDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int MenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}
