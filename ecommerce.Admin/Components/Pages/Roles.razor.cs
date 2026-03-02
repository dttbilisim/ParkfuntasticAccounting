using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class Roles
{
    [Inject] private IRoleService RoleService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private RadzenDataGrid<RoleListDto> grid;
    private List<RoleListDto> RolesList { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadRoles();
    }

    private async Task LoadRoles()
    {
        var result = await RoleService.GetAllRoles();
        if (result.Ok)
        {
            RolesList = result.Result!;
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

    private async Task OpenUpsertModal(int? id = null)
    {
        var result = await DialogService.OpenAsync<UpsertRole>(
            id.HasValue ? "Rol Düzenle" : "Yeni Rol Ekle",
            new Dictionary<string, object> { { "Id", id } },
            new DialogOptions { Width = "1000px", CloseDialogOnOverlayClick = true }
        );

        if (result == true)
        {
            await LoadRoles();
            await grid.Reload();
        }
    }

    private async Task DeleteRole(RoleListDto role)
    {
        var confirm = await DialogService.Confirm("Bu rolü silmek istediğinize emin misiniz?", "Rol Sil", new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
        if (confirm == true)
        {
            var result = await RoleService.DeleteRole(role.Id);
            if (result.Ok)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Rol başarıyla silindi."
                });
                await LoadRoles();
                await grid.Reload();
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
    }
}
