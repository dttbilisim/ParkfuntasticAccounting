using ecommerce.Core.Utils;

namespace ecommerce.Core.Dtos;


public class UserClaims
{
    public string UserId { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public string CompanyName { get; set; }
    public string Expiration { get; set; }
    
    public WebUserType UserType { get; set; }
    
}