namespace ecommerce.Core.Models {
    public class ApplicationClaim {
        public string Type { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public partial class ApplicationAuthenticationState {
        public bool IsAuthenticated { get; set; } = false;
        public string? Name { get; set; } 
        public IEnumerable<ApplicationClaim> Claims { get; set; } = null!;
    }
}
