using AutoMapper;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;

namespace ecommerce.Admin.Domain.Concreate;

public class IdentityUserService : IIdentityUserService
{
    public const string DefaultAdminUserName = "Admin";

    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly IRepository<ApplicationRole> _roleRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly ITenantProvider _tenantProvider;

    public IdentityUserService(
        IUnitOfWork<ApplicationDbContext> context,
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger logger,
        CurrentUser currentUser,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;

        _roleRepository = context.GetRepository<ApplicationRole>();
        _userRepository = context.GetRepository<ApplicationUser>();
    }

    public async Task<IActionResult<Paging<List<IdentityUserListDto>>>> GetPagedListAsync(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<IdentityUserListDto>>>();

        try
        {
            var query = _userRepository.GetAll(true)
                .Where(u => u.MembershipId == null && u.CompanyId == null)
                .Include(u => u.Roles)
                .AsQueryable();

            // Multi-tenant filtering: Branch admins see users in their corporation
            if (!_tenantProvider.IsGlobalAdmin)
            {
                var currentCorpId = _tenantProvider.GetCurrentCorporationId();
                if (currentCorpId > 0)
                {
                    // Filter users who have at least one branch in the current corporation
                    // AND only show users with specific "manageable" roles (CustomerB2B and B2BADMIN)
                    query = from u in query
                            join ub in _context.DbContext.UserBranches on u.Id equals ub.UserId
                            where ub.Branch.CorporationId == currentCorpId
                            where u.Roles.Any(r => r.Name == "CustomerB2B" || r.Name.ToUpper() == "B2BADMIN" || r.Name.ToUpper() == "ACCOUNTANT" || r.Name == "Accountant" || r.Name == "Plasiyer")
                            select u;
                    
                    query = query.Distinct();
                }
            }
            
            // Materialize the list first to avoid DbContext disposal issues
            var users = await query.ToListAsync();
            
            // Now map to DTOs
            var userDtos = _mapper.Map<List<IdentityUserListDto>>(users);
            
            response.Result = new Paging<List<IdentityUserListDto>>
            {
                Data = userDtos,
                DataCount = userDtos.Count
            };
        }
        catch (Exception e)
        {
            _logger.LogError("GetPagedListAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<List<IdentityRoleListDto>>> GetRoleListAsync()
    {
        var response = OperationResult.CreateResult<List<IdentityRoleListDto>>();

        try
        {
            var roles = await _context.DbContext.Roles.AsNoTracking().ToListAsync();
            response.Result = _mapper.Map<List<IdentityRoleListDto>>(roles);
        }
        catch (Exception e)
        {
            _logger.LogError("GetRoleListAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<IdentityUserUpsertDto>> GetAsync(int Id)
    {
        var response = OperationResult.CreateResult<IdentityUserUpsertDto>();

        try
        {
            var entity = await _userRepository.GetFirstOrDefaultAsync(
                predicate: f => f.Id == Id && f.MembershipId == null && f.CompanyId == null,
                include: q => q.Include(i => i.Roles)
            );

            if (entity == null)
            {
                response.AddError("Kullanıcı bulunamadı.");
                return response;
            }

            response.Result = _mapper.Map<IdentityUserUpsertDto>(entity);
        }
        catch (Exception e)
        {
            _logger.LogError("GetAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                response.AddError("Kullanıcı bulunamadı.");
                return response;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);

            if (!result.Succeeded)
            {
                response.AddError(string.Join(Environment.NewLine, result.Errors.Select(s => s.Description)));
            }
        }
        catch (Exception e)
        {
            _logger.LogError("ChangePasswordAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<int>> UpsertAsync(IdentityUserUpsertDto dto)
    {
        var response = OperationResult.CreateResult<int>();

        try
        {
            var entity = dto.Id.HasValue
                ? await _userRepository.GetFirstOrDefaultAsync(
                    predicate: r => r.Id == dto.Id,
                    include: q => q.Include(i => i.Roles),
                    disableTracking: false
                )
                : new ApplicationUser();

            if (entity == null)
            {
                response.AddError("Kullanıcı bulunamadı.");
                return response;
            }

            entity = _mapper.Map(dto, entity);

            IdentityResult identityResult;

            if (entity.Id > 0)
            {
                identityResult = await _userManager.UpdateAsync(entity);

                if (entity.NormalizedUserName != _userManager.NormalizeName(DefaultAdminUserName))
                {
                    var userNameResult = await _userManager.SetUserNameAsync(entity, dto.UserName);

                    if (!userNameResult.Succeeded)
                    {
                        response.AddError(string.Join(Environment.NewLine, userNameResult.Errors.Select(s => s.Description)));
                        return response;
                    }
                }

                if (entity.NormalizedEmail != _userManager.NormalizeEmail(dto.Email))
                {
                    var emailResult = await _userManager.SetEmailAsync(entity, dto.Email);

                    if (!emailResult.Succeeded)
                    {
                        response.AddError(string.Join(Environment.NewLine, emailResult.Errors.Select(s => s.Description)));
                        return response;
                    }
                }

                if (!string.Equals(entity.PhoneNumber, dto.PhoneNumber, StringComparison.InvariantCultureIgnoreCase))
                {
                    var phoneResult = await _userManager.SetPhoneNumberAsync(entity, dto.PhoneNumber);

                    if (!phoneResult.Succeeded)
                    {
                        response.AddError(string.Join(Environment.NewLine, phoneResult.Errors.Select(s => s.Description)));
                        return response;
                    }
                }

                if (identityResult.Succeeded && !string.IsNullOrEmpty(dto.Password))
                {
                    var passwordErrors = new List<IdentityError>();

                    foreach (var validator in _userManager.PasswordValidators)
                    {
                        var result = await validator.ValidateAsync(_userManager, entity, dto.Password);

                        if (!result.Succeeded)
                        {
                            passwordErrors.AddRange(result.Errors);
                        }
                    }

                    if (passwordErrors.Any())
                    {
                        response.AddError(string.Join(Environment.NewLine, passwordErrors.Select(s => s.Description)));
                        return response;
                    }

                    await _userManager.RemovePasswordAsync(entity);
                    identityResult = await _userManager.AddPasswordAsync(entity, dto.Password);
                }
            }
            else
            {
                entity.UserName = dto.UserName;
                entity.Email = dto.Email;
                entity.PhoneNumber = dto.PhoneNumber;

                identityResult = await _userManager.CreateAsync(entity, dto.Password!);
            }

            if (!identityResult.Succeeded)
            {
                response.AddError(string.Join(Environment.NewLine, identityResult.Errors.Select(s => s.Description)));
                return response;
            }

            var roles = await _roleRepository.GetAllAsync(true);
            var userRoles = entity.Roles.Select(s => s.Id).ToList();

            var addedRoles = dto.Roles.Except(userRoles).ToList();
            var removedRoles = userRoles.Except(dto.Roles).ToList();

            if (removedRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(entity, roles.Where(w => removedRoles.Contains(w.Id)).Select(s => s.Name!));
            }

            if (addedRoles.Any())
            {
                await _userManager.AddToRolesAsync(entity, roles.Where(w => addedRoles.Contains(w.Id)).Select(s => s.Name!));
            }

            if (removedRoles.Any() || addedRoles.Any())
            {
                await _userManager.UpdateSecurityStampAsync(entity);
            }

            response.Result = entity.Id;
        }
        catch (Exception e)
        {
            _logger.LogError("UpsertAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> DeleteAsync(int id)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            var entity = await _userRepository.GetFirstOrDefaultAsync(predicate: r => r.Id == id, disableTracking: false);

            if (entity == null)
            {
                response.AddError("Kullanıcı bulunamadı.");
                return response;
            }

            if (entity.NormalizedUserName == _userManager.NormalizeName(DefaultAdminUserName))
            {
                response.AddError("Ana yönetici hesabı silinemez.");
                return response;
            }

            if (_currentUser.Id == entity.Id)
            {
                response.AddError("Kendi hesabınızı silemezsiniz.");
                return response;
            }

            var identityResult = await _userManager.DeleteAsync(entity);

            if (!identityResult.Succeeded)
            {
                response.AddError(string.Join(Environment.NewLine, identityResult.Errors.Select(s => s.Description)));
            }
        }
        catch (Exception e)
        {
            _logger.LogError("DeleteAsync Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }
}