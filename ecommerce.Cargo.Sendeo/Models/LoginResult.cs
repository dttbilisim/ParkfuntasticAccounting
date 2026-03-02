namespace ecommerce.Cargo.Sendeo.Models
{
    public class LoginResult
    {
        public int CustomerId { get; set; }

        public string CustomerTitle { get; set; } = null!;

        public string Token { get; set; } = null!;
    }
}