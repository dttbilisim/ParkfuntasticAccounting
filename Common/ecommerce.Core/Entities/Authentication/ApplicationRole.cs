using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace ecommerce.Core.Entities.Authentication {
    public class ApplicationRole : IdentityRole<int> {
        [JsonIgnore]
        public ICollection<ApplicationUser> Users { get; set; } = null!;

    }
}
