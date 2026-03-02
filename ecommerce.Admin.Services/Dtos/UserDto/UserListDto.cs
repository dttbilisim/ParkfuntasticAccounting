using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.UserDto
{
    public class UserListDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public WebUserType WebUserType { get; set; }
        public bool IsAproved { get; set; }
    }
}


