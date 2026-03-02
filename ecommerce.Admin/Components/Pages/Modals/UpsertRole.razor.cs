using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Admin.Domain.Dtos.RoleMenuDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertRole
{
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private IRoleMenuService RoleMenuService { get; set; } = default!;
    [Inject] private IMenuService MenuService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private ILogger<UpsertRole> Logger { get; set; } = default!;

    [Parameter] public int? Id { get; set; }

    private RoleUpsertDto Role { get; set; } = new();
    private List<Menu> AllMenus { get; set; } = new();
    private List<RoleMenuUpsertDto> RoleMenuPermissions { get; set; } = new();
    private bool Saving { get; set; }
    private bool SavingMenus { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load all menus for the tree
        var menuResponse = await MenuService.GetMenuHierarchy();
        if (menuResponse.Ok)
        {
            AllMenus = menuResponse.Result ?? new List<Menu>();
        }

        if (Id.HasValue && Id.Value > 0)
        {
            var result = await RoleService.GetRoleById(Id.Value);
            if (result.Ok)
            {
                Role = result.Result!;
                
                // Load role's menu permissions
                var roleMenusResponse = await RoleMenuService.GetRoleMenus(Id.Value);
                if (roleMenusResponse.Ok && roleMenusResponse.Result != null)
                {
                    RoleMenuPermissions = roleMenusResponse.Result.Select(x => new RoleMenuUpsertDto
                    {
                        Id = x.Id,
                        MenuId = x.MenuId,
                        RoleId = x.RoleId,
                        CanView = x.CanView,
                        CanCreate = x.CanCreate,
                        CanEdit = x.CanEdit,
                        CanDelete = x.CanDelete
                    }).ToList();
                }
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Bir hata oluştu"
                });
                DialogService.Close();
            }
        }
    }

    private async Task FormSubmit(RoleUpsertDto args)
    {
        Saving = true;
        try
        {
            var result = await RoleService.UpsertRole(args);
            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Rol başarıyla kaydedildi."
                });
                
                if (!Id.HasValue && RoleMenuPermissions.Any())
                {
                     NotificationService.Notify(NotificationSeverity.Info, "Rol kaydedildi. Menü yetkileri için lütfen 'Menü Yetkileri' sekmesini kullanın.");
                }

                DialogService.Close(true);
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Bir hata oluştu"
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Beklenmeyen bir hata oluştu."
            });
        }
        finally
        {
            Saving = false;
        }
    }

    private async Task SaveMenuPermissions()
    {
        if (!Id.HasValue && (Role.Id == 0 || Role.Id == null)) // Ensure Role ID exists or check if we are in Edit mode
        {
            // Note: If newly created role, user has to save role first, then modal closes. 
            // They have to reopen to set permissions. This is slightly bad UX but standard for now.
            // Improve: Return ID from UpsertRole and keep modal open? 
            // Current UpsertRole returns Empty.
            NotificationService.Notify(NotificationSeverity.Warning, "Önce rolü kaydedin (Eğer yeni rol ekliyorsanız önce kaydedip sonra düzenleyerek yetki verebilirsiniz).");
            return;
        }
        
        SavingMenus = true;
        
        // Filter out permissions where everything is false (optional cleanup)
        var permissionsToSave = RoleMenuPermissions
            .Where(x => x.CanView || x.CanCreate || x.CanEdit || x.CanDelete)
            .ToList();
            
        var response = await RoleMenuService.UpsertRoleMenus(Id.Value, permissionsToSave);
        if (response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Rol menü yetkileri kaydedildi");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        SavingMenus = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPermissionChanged()
    {
        await InvokeAsync(StateHasChanged);
    }
    
    private async Task ReloadMenus()
    {
        var menuResponse = await MenuService.GetMenuHierarchy();
        if (menuResponse.Ok)
        {
            AllMenus = menuResponse.Result ?? new List<Menu>();
            await InvokeAsync(StateHasChanged);
        }
    }
}
