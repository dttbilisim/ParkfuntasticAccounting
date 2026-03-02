using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace ecommerce.Core.Helpers {
    public sealed class IdentityHelper {
        private IdentityHelper() { }

        public static IdentityHelper Instance => Lazy.Value;

        public void Configure(IHttpContextAccessor httpContextAccessor) {
            ContextAccessor = httpContextAccessor ?? throw new ArgumentException(nameof(IHttpContextAccessor));
            IsInitialized = true;
        }

        public int UserId { get { return int.TryParse(ContextAccessor?.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value, out var intUserId) ? intUserId : 0; } }

        private static readonly Lazy<IdentityHelper> Lazy = new Lazy<IdentityHelper>(() => new IdentityHelper());

        private bool IsInitialized { get; set; }

        private static IHttpContextAccessor ContextAccessor { get; set; } = null!;

    }
}
