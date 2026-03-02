namespace ecommerce.Core.Dtos.Login;

public class LoginModelRequestDto
{
    public string Email { get; set; }
    public string Password { get; set; }

    public bool IsAproved { get; set; }
    
}