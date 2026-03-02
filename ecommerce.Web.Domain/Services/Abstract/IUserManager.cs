using ecommerce.Core.Dtos.Login;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Identity;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IUserManager
{
    Task<IActionResult<User>> CreateUserAsync(User model);
    Task<IActionResult<User>> LoginAsync(LoginModelRequestDto loginModel);
    Task<IActionResult<UserAddress>> UpsertUserAddressAsync(UserAddress model);
    Task<IActionResult<List<UserAddress>>> GetAllUserAddressesAsync();
    Task<IActionResult<string>> DeleteUserAddressAsync(int addressId);
    Task<IActionResult<string>> SetDefaultAddressAsync(int addressId);
    Task<IActionResult<string>> ForgotPasswordAsync(string email);
    Task<IActionResult<User>> UpdateUserProfileAsync(User updatedUser);
    Task<IActionResult<User>> GetByIdAsync(int userId);
    Task<IActionResult<User>> GetCurrentUserAsync();
  
    Task<IActionResult<string>> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<User?> GetUserByEmailAsync(string email);
    Task<IActionResult<string>> ActivateAccountAsync(string token, string email);
    Task<IdentityResult> ResetUserPasswordAsync(User user, string newPassword);
    Task UpdateUserAsync(User user);
    Task EnsureCompanyForUserAsync(User user, bool isConfirmed = true, bool isEmailConfirmed = true);
}