using ecommerce.Admin.Domain.Dtos.MenuDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Components.Shared;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class Menus
{
    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IMenuService MenuService { get; set; }

    private AdminDataGrid<MenuListDto> Grid { get; set; }
    private List<MenuListDto> GridItems { get; set; }
    private int GridCount { get; set; }

    private async Task OpenUpsertModal(MenuListDto? args = null)
    {
        var result = await DialogService.OpenAsync<UpsertMenu>(
            args == null ? "Menü Ekle" : "Menü Düzenle - " + args.Name,
            new Dictionary<string, object?>
            {
                { nameof(UpsertMenu.Id), args?.Id }
            },
            options: new DialogOptions
            {
                Width = "600px",
                CssClass = "mw-100"
            }
        );

        if (result != null)
        {
            await Grid.Reload();
        }
    }

    private async Task DeleteRow(MenuListDto dto)
    {
        if (await DialogService.Confirm(
                "Bu menüyü silmek istediğinizden emin misiniz?",
                "Menü Sil",
                new ConfirmOptions
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }
            ) == true)
        {
            var response = await MenuService.DeleteMenu(dto.Id.GetValueOrDefault());

            if (response.Ok)
            {
                await Grid.Reload();
                NotificationService.Notify(NotificationSeverity.Success, "Menü başarıyla silindi");
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
    }

    private async Task LoadData(LoadDataArgs args)
    {
        try
        {
            // Filter ve OrderBy null-safe
            var filter = string.IsNullOrWhiteSpace(args.Filter) ? null : args.Filter;
            var orderBy = string.IsNullOrWhiteSpace(args.OrderBy) ? "Id desc" : args.OrderBy;
            
            var pager = new PageSetting(filter, orderBy, args.Skip, args.Top);
            var response = await MenuService.GetPagedMenus(pager);
            if (response.Ok && response.Result != null && response.Result.Data != null)
            {
                GridItems = response.Result.Data;
                GridCount = response.Result.DataCount;
            }
            else
            {
                GridItems = new List<MenuListDto>();
                GridCount = 0;
                if (!response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Menus.LoadData] EXCEPTION: {ex.Message}");
            GridItems = new List<MenuListDto>();
            GridCount = 0;
        }
        StateHasChanged();
    }
}
