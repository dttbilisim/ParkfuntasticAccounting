using AutoMapper;
using ecommerce.Admin.Domain.Dtos.MenuDto;
using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertMenu
{
    [Inject] private DialogService DialogService { get; set; }
    [Inject] private NotificationService NotificationService { get; set; }
    [Inject] private IMapper Mapper { get; set; }
    [Inject] private IMenuService MenuService { get; set; }
    [Inject] private IRoleService RoleService { get; set; }
    
    [Parameter] public int? Id { get; set; }
    
    private MenuUpsertDto EditingEntity { get; set; } = new();
    private List<Menu> AvailableParentMenus { get; set; } = new();
    private List<RoleListDto> AvailableRoles { get; set; } = new();
    private bool Saving { get; set; }

    protected override async Task OnInitializedAsync()
    {
        // Load all menus for parent dropdown
        var allMenusResponse = await MenuService.GetAllMenus();
        if (allMenusResponse.Ok && allMenusResponse.Result != null)
        {
            AvailableParentMenus = allMenusResponse.Result
                .DistinctBy(m => m.Id)
                .ToList();
            
            // If editing, exclude self and descendants from parent dropdown
            if (Id.HasValue)
            {
                AvailableParentMenus = AvailableParentMenus.Where(m => m.Id != Id.Value).ToList();
            }
        }
        
        // Load Roles - DistinctBy ensures unique keys for RadzenDropDown (avoids "duplicate key" render error)
        var rolesResponse = await RoleService.GetAllRoles();
        if (rolesResponse.Ok && rolesResponse.Result != null)
        {
            AvailableRoles = rolesResponse.Result
                .DistinctBy(r => r.Id)
                .ToList();
        }
        
        if (Id.HasValue)
        {
            var response = await MenuService.GetMenuById(Id.Value);
            if (!response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                DialogService.Close();
                return;
            }
            EditingEntity = response.Result;
        }
        
        await InvokeAsync(StateHasChanged);
    }

    private async Task FormSubmit()
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(EditingEntity.Name))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Menü adı gereklidir");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingEntity.Path))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Yol gereklidir");
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingEntity.Icon))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "İkon gereklidir");
            return;
        }

        Saving = true;
        var response = await MenuService.UpsertMenu(EditingEntity);
        if (response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Menü başarıyla kaydedildi");
            DialogService.Close(EditingEntity);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        Saving = false;
    }

    private void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close();
    }
}
