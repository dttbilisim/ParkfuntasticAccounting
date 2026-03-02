using ecommerce.Core.Dtos.Login;
using ecommerce.Core.Entities.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Authorization;

using ecommerce.Domain.Shared.Abstract;

namespace ecommerce.Web.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(SignInManager<User> signInManager, UserManager<User> userManager, IUserClaimsPrincipalFactory<User> claimsFactory, IRedisCacheService redisCacheService) : ControllerBase
{
    [HttpPost("login")]
    [IgnoreAntiforgeryToken] // API call from JS
    public async Task<IActionResult> Login([FromBody] LoginModelRequestDto model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest("E-posta ve şifre zorunludur.");
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Security: Don't reveal user existence
            return BadRequest("Geçersiz e-posta veya şifre.");
        }

        // Check password and sign in
        var result = await signInManager.PasswordSignInAsync(user, model.Password, true, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Redis Online User Tracking
            try 
            {
                var userId = user.Id.ToString();
                
                // 1. Sortable set for counting/listing IDs
                await redisCacheService.AddToSortedSetAsync("online_users", userId, DateTime.UtcNow.Ticks);
                
                // 2. Detail object for the dashboard widget
                var onlineUser = new ecommerce.Domain.Shared.Dtos.OnlineUserDto 
                {
                    UserId = user.Id.ToString(),
                    Username = $"{user.FirstName} {user.LastName}",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                    LastActiveTime = DateTime.UtcNow,
                    LastPageUrl = "/login"
                };

                await redisCacheService.SetAsync($"online_user_detail:{userId}", onlineUser, TimeSpan.FromMinutes(65)); // Slightly more than widget's 60m lookback
            }
            catch (Exception ex)
            {
                // Redis failure should not block login
                Console.WriteLine($"Redis online track error: {ex.Message}");
            }

            return Ok(new { ok = true });
        }
        
        if (result.IsLockedOut)
        {
            return BadRequest("Hesabınız kilitlendi. Lütfen daha sonra tekrar deneyin.");
        }

        return BadRequest("Geçersiz e-posta veya şifre.");
    }

    [HttpPost("logout")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Get UserId before signing out
        var userId = userManager.GetUserId(User);
        
        // Sign out from Identity cookie and clear authentication
        await signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        
        // Cleanup Redis
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                await redisCacheService.RemoveFromSortedSetAsync("online_users", userId);
                await redisCacheService.RemoveAsync($"online_user_detail:{userId}");
            }
            catch
            {
                // fail silently
            }
        }
        
        return NoContent();
    }
}


