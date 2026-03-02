using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IUserCarService
{
    Task<IActionResult<List<UserCars>>> GetAllUserCarsAsync();
    Task<IActionResult<List<UserCars>>> GetAllUserCarsByUserIdAsync(int userId);
    Task<IActionResult<List<UserCars>>> GetAllUserCarsForCurrentUserAsync();
    Task<IActionResult<UserCars>> UpsertUserCarAsync(UserCars userCar);
    Task<IActionResult<UserCars>> DeleteUserCarAsync(int id);
}