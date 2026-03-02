using AutoMapper;
using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "roles";

    public RoleService(RoleManager<ApplicationRole> roleManager, IMapper mapper, ILogger<RoleService> logger, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _roleManager = roleManager;
        _mapper = mapper;
        _logger = logger;
        _permissionService = permissionService;
    }

    public async Task<IActionResult<List<RoleListDto>>> GetAllRoles()
    {
        var rs = new IActionResult<List<RoleListDto>>();
        try
        {
            var roles = await _roleManager.Roles.ToListAsync();
            var dtos = _mapper.Map<List<RoleListDto>>(roles);
            rs.Result = dtos ?? new List<RoleListDto>();
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles");
            rs.AddError("Roller yüklenirken bir hata oluştu.");
            return rs;
        }
    }

    public async Task<IActionResult<RoleUpsertDto>> GetRoleById(int id)
    {
        var rs = new IActionResult<RoleUpsertDto>();
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                rs.AddError("Rol bulunamadı.");
                return rs;
            }

            var dto = _mapper.Map<RoleUpsertDto>(role);
            rs.Result = dto;
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting role {id}");
            rs.AddError("Rol bilgisi alınırken hata oluştu.");
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertRole(RoleUpsertDto roleDto)
    {
        var rs = new IActionResult<Empty>();
        try
        {
            ApplicationRole? role;

            if (roleDto.Id.HasValue && roleDto.Id > 0)
            {
                // Update
                role = await _roleManager.FindByIdAsync(roleDto.Id.Value.ToString());
                if (role == null)
                {
                    rs.AddError("Güncellenecek rol bulunamadı.");
                    return rs;
                }

                role.Name = roleDto.Name;
                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    rs.AddError(string.Join(", ", result.Errors.Select(e => e.Description)));
                    return rs;
                }
            }
            else
            {
                // Create
                role = new ApplicationRole { Name = roleDto.Name };
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    rs.AddError(string.Join(", ", result.Errors.Select(e => e.Description)));
                    return rs;
                }
            }

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting role");
            rs.AddError("Rol kaydedilirken bir hata oluştu.");
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteRole(int id)
    {
        var rs = new IActionResult<Empty>();
        try
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                rs.AddError("Silinecek rol bulunamadı.");
                return rs;
            }

            var result = await _roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                rs.AddError(string.Join(", ", result.Errors.Select(e => e.Description)));
                return rs;
            }

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting role {id}");
            rs.AddError("Rol silinirken bir hata oluştu.");
            return rs;
        }
    }
}
