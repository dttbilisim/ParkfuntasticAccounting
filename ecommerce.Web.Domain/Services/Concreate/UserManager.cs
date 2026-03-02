using Blazored.LocalStorage;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Dtos;
using ecommerce.Core.Dtos.Login;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using IEmailService = ecommerce.Domain.Shared.Emailing.IEmailService;
using System.Threading;
using System.Security.Claims;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Identity;

namespace ecommerce.Web.Domain.Services.Concreate;

public class UserManager : IUserManager
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IEmailService _emailService;
    private readonly ILocalStorageService _localStorageService;
    private readonly IConfiguration _configuration;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITenantProvider _tenantProvider;
    private readonly CurrentUser _currentUser;

    private static readonly SemaphoreSlim _addressLoadLock = new(1, 1);

    public UserManager(IUnitOfWork<ApplicationDbContext> context, IEmailService emailService,
        ILocalStorageService localStorage, IConfiguration configuration, UserManager<User> userManager,
        SignInManager<User> signInManager, ITenantProvider tenantProvider, CurrentUser currentUser)
    {
        _context = context;
        _emailService = emailService;
        _localStorageService = localStorage;
        _configuration = configuration;
        _configuration = configuration;
        _userManager = userManager;
        _signInManager = signInManager;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
    }

//KAYIT OLMA
    public async Task<IActionResult<User>> CreateUserAsync(User model)
    {
        try
        {
            var rs = OperationResult.CreateResult<User>();
            model.Email = model.Email.Trim();

            var checkUser = await _signInManager.UserManager.FindByEmailAsync(model.Email);
            if (checkUser != null)
            {
                rs.AddError("EmailAlready");
                return rs;
            }

            model.UserName = model.Email;
            model.RegisterDate = DateTime.UtcNow;
            model.IsAproved = false;

            var isType2 = model.WebUserType == (ecommerce.Core.Utils.WebUserType)2;
            model.NewUserToken = isType2 ? null : Guid.NewGuid().ToString("N").ToUpper();

            var createResult = await _signInManager.UserManager.CreateAsync(model, model.PasswordHash);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    rs.AddError(error.Description);
                }

                return rs;
            }


            if (!isType2)
            {
                await _emailService.SendNewUserEmail(
                    $"{model.FirstName} {model.LastName}",
                    model.Email,
                    model.NewUserToken);
            }
            else
            {
                await EnsureCompanyForUserAsync(model, isConfirmed: false, isEmailConfirmed: false);
            }

            rs.Result = model;
            return rs;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

//LOGİN
    public async Task<IActionResult<User>> LoginAsync(LoginModelRequestDto loginModel)
    {
        var rs = OperationResult.CreateResult<User>();
        loginModel.Email = loginModel.Email.Trim().ToLower();

        var userRepo = _context.GetRepository<User>();
        var user = await userRepo.GetFirstOrDefaultAsync(predicate: x => x.Email == loginModel.Email);
        if (user == null)
        {
            rs.Result = new User();
            rs.AddError("Kullanıcı bulunamadı.");
            return rs;
        }

        if (!user.IsAproved)
        {
            rs.Result = new User();
            rs.AddError("Hesabınız henüz onaylanmamış.");
            return rs;
        }

        var signInResult = await _signInManager.PasswordSignInAsync(user, loginModel.Password, isPersistent: true, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            rs.Result = new User();
            rs.AddError("Şifre hatalı.");
            return rs;
        }

        user.LastLoginDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        rs.Result = user;
        return rs;
    }
    // Adres

    public async Task<IActionResult<List<UserAddress>>> GetAllUserAddressesAsync()
    {
        var rs = OperationResult.CreateResult<List<UserAddress>>();
        await _addressLoadLock.WaitAsync();
        try
        {
            var userIdStr = _currentUser.Id?.ToString();
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                rs.AddError("Unauthorized");
                return rs;
            }
            // Use a short-lived DbContext to avoid shared-connection contention
            var rawCs = _configuration.GetConnectionString("ApplicationDbContext");
            var csb = new Npgsql.NpgsqlConnectionStringBuilder(rawCs)
            {
                KeepAlive = 30,
                MaxPoolSize = 200,
                Multiplexing = false
            };
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .UseNpgsql(csb.ConnectionString, o =>
                {
                    o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                     .MigrationsAssembly("ecommerce.EFCore");
                    o.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null);
                    o.CommandTimeout(180);
                });
            await using (var db = new ApplicationDbContext(optionsBuilder.Options, _tenantProvider))
            {
                var userAdress = await db.Set<UserAddress>()
                    .AsNoTracking()
                    .Include(x => x.City)
                    .Include(x => x.Town)
                    .Where(x => x.UserId == currentUserId)
                    .ToListAsync();
                rs.Result = userAdress;
            }
            return rs;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _addressLoadLock.Release();
        }
    }

    public async Task<IActionResult<UserAddress>> UpsertUserAddressAsync(UserAddress model)
    {
        var rs = OperationResult.CreateResult<UserAddress>();

        try
        {
            
            var userIdStr = _currentUser.Id?.ToString();
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
            
                if (model.UserId.HasValue && model.UserId.Value > 0)
                {
                    userIdStr = model.UserId.Value.ToString();
                    currentUserId = model.UserId.Value;
                }
                else
                {
                    rs.AddError("Unauthorized");
                    return rs;
                }
            }

            var user = await _signInManager.UserManager.FindByIdAsync(userIdStr);
            if (user == null)
            {
                rs.AddError("Kullanıcı bulunamadı");
                return rs;
            }

            model.UserId = currentUserId; // Web context uses UserId

            var repo = _context.GetRepository<UserAddress>();

            if (model.Id == 0)
            {
                model.CreatedDate = DateTime.UtcNow;
                model.CreatedId = currentUserId;
                await repo.InsertAsync(model);
            }
            else
            {
                var existing = await repo.FindAsync(model.Id);
                if (existing == null)
                {
                    rs.AddError("Adres kaydı bulunamadı");
                    return rs;
                }

                existing.UserId = currentUserId; // Web context uses UserId
                existing.AddressName = model.AddressName;
                existing.FullName = model.FullName;
                existing.Email = model.Email;
                existing.PhoneNumber = model.PhoneNumber;
                existing.CityId = model.CityId;
                existing.TownId = model.TownId;
                existing.Address = model.Address;
                existing.IdentityNumber = model.IdentityNumber; // TC Kimlik No
                existing.IsDefault = model.IsDefault; // Varsayılan adres
                
                // Invoice Address Fields
                existing.IsSameAsDeliveryAddress = model.IsSameAsDeliveryAddress;
                existing.InvoiceCityId = model.InvoiceCityId;
                existing.InvoiceTownId = model.InvoiceTownId;
                existing.InvoiceAddress = model.InvoiceAddress;

              
                existing.ModifiedDate = DateTime.UtcNow;
                existing.ModifiedId = currentUserId;


                repo.Update(existing);
                model = existing;
            }

            await _context.SaveChangesAsync();
            rs.Result = model;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<string>> DeleteUserAddressAsync(int addressId)
    {
        var rs = OperationResult.CreateResult<string>();
        try
        {
            var repo = _context.GetRepository<UserAddress>();
            var existing = await repo.FindAsync(addressId);
            if (existing == null)
            {
                rs.AddError("Adres kaydı bulunamadı");
                return rs;
            }

            repo.Delete(existing);
            await _context.SaveChangesAsync();
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }
    
    public async Task<IActionResult<string>> SetDefaultAddressAsync(int addressId)
    {
        var rs = OperationResult.CreateResult<string>();
        try
        {
            var userIdStr = _currentUser.Id?.ToString();
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var currentUserId))
            {
                rs.AddError("Unauthorized");
                return rs;
            }

            var repo = _context.GetRepository<UserAddress>();
            
            // Get all user addresses using DbSet
            var userAddresses = await _context.DbContext.Set<UserAddress>()
                .Where(x => x.UserId == currentUserId)
                .ToListAsync();
            
            // Set all to non-default
            foreach (var addr in userAddresses)
            {
                addr.IsDefault = false;
                repo.Update(addr);
            }
            
            // Set selected address as default
            var selectedAddress = userAddresses.FirstOrDefault(x => x.Id == addressId);
            if (selectedAddress == null)
            {
                rs.AddError("Adres kaydı bulunamadı");
                return rs;
            }
            
            selectedAddress.IsDefault = true;
            repo.Update(selectedAddress);
            
            await _context.SaveChangesAsync();
            rs.Result = "Varsayılan adres ayarlandı";
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<string>> ForgotPasswordAsync(string email)
    {
        var rs = OperationResult.CreateResult<string>();
        try
        {
            var user = await _signInManager.UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                rs.AddError("Email adresi ile eşleşen kullanıcı bulunamadı.");
                return rs;
            }

            user.ResetEmailToken = Guid.NewGuid().ToString("N").ToUpper();
            user.ResetEmailTokenExpireDate = DateTime.UtcNow.AddMinutes(30);
            await _signInManager.UserManager.UpdateAsync(user);

            await _emailService.SendNewUserTokenEmail(
                $"{user.FirstName} {user.LastName}",
                email,
                user.ResetEmailToken
            );

            rs.Result = "Şifre sıfırlama bağlantısı e-posta adresinize gönderildi.";
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<User>> UpdateUserProfileAsync(User updatedUser)
    {
        var rs = OperationResult.CreateResult<User>();

        try
        {
            var user = await _signInManager.UserManager.FindByIdAsync(updatedUser.Id.ToString());
            if (user == null)
            {
                rs.AddError("Kullanıcı bulunamadı.");
                return rs;
            }

            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.MiddleName = updatedUser.MiddleName;
            user.BirthDate = updatedUser.BirthDate;
            user.CompanyName = updatedUser.CompanyName;
            user.FileDocumenturl = updatedUser.FileDocumenturl;

            var result = await _signInManager.UserManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    rs.AddError(error.Description);
                }

                return rs;
            }

            rs.Result = user;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<User>> GetByIdAsync(int userId)
    {
        var rs = OperationResult.CreateResult<User>();
        try
        {
            var user = await _signInManager.UserManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                rs.AddError("Kullanıcı bulunamadı");
                return rs;
            }

            rs.Result = user;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<User>> GetCurrentUserAsync()
    {
        var rs = OperationResult.CreateResult<User>();
        try
        {
            var currentUser = await _signInManager.UserManager.GetUserAsync(_currentUser.Principal);
            if (currentUser == null)
            {
                rs.AddError("Unauthorized");
                return rs;
            }
            rs.Result = currentUser;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

   

    public async Task<IActionResult<string>> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var rs = OperationResult.CreateResult<string>();
        try
        {
            var user = await _signInManager.UserManager.GetUserAsync(_currentUser.Principal);
            if (user == null)
            {
                rs.AddError("Kullanıcı bulunamadı.");
                return rs;
            }

            var result = await _signInManager.UserManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    rs.AddError(error.Description);
                }

                return rs;
            }

            rs.Result = "Şifre başarıyla değiştirildi.";
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task EnsureCompanyForUserAsync(User user, bool isConfirmed = true, bool isEmailConfirmed = true)
    {
        var companyRepo = _context.GetRepository<Company>();

        var existing = await companyRepo.GetFirstOrDefaultAsync(predicate: c => c.FkId == user.Id);
        if (existing != null) return;


        var companyWorkingType =
            user.WebUserType == (ecommerce.Core.Utils.WebUserType)1
                ? (ecommerce.Core.Utils.CompanyWorkingType)3
                : user.WebUserType == (ecommerce.Core.Utils.WebUserType)2
                    ? (ecommerce.Core.Utils.CompanyWorkingType)2
                    : default;

        var company = new Company
        {
            UserType = (ecommerce.Core.Utils.UserType)3,
            CompanyWorkingType = companyWorkingType,

            FirstName = user.FirstName,
            LastName = user.LastName,
            BirthDate = user.BirthDate,
            EmailAddress = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,

            Status = 1,
            Address = string.Empty,
            CityId = 1,
            TownId = 1,

            PharmacyName = user.CompanyName,
            FileDocumenturl = user.FileDocumenturl,

            FkId = user.Id,

            IsConfirmed = isConfirmed,
            IsEmailConfirmed = isEmailConfirmed
        };

        await companyRepo.InsertAsync(company);
        await _context.SaveChangesAsync();
    }

    public async Task<IActionResult<string>> ActivateAccountAsync(string token, string email)
    {
        var rs = OperationResult.CreateResult<string>();
        var user = await _signInManager.UserManager.FindByEmailAsync(email);
        if (user == null || user.NewUserToken != token)
        {
            rs.AddError("Geçersiz token veya e-posta");
            return rs;
        }

        user.IsAproved = true;
        user.NewUserToken = null;
        await _signInManager.UserManager.UpdateAsync(user);
        await EnsureCompanyForUserAsync(user);

        return rs;
    }

    public async Task<IdentityResult> ResetUserPasswordAsync(User user, string newPassword)
    {
        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
            return removeResult;

        return await _userManager.AddPasswordAsync(user, newPassword);
    }

    public async Task UpdateUserAsync(User user)
    {
        await _userManager.UpdateAsync(user);
    }
}