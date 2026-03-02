using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Resources;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class ApplicationUsers
{
    [Inject]
    private DialogService DialogService { get; set; }

    [Inject]
    private NotificationService NotificationService { get; set; }

    [Inject]
    private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

    [Inject]
    private IIdentityUserService IdentityUserService { get; set; }

    [Inject]
    private ComponentDebouncer ComponentDebouncer { get; set; }

    private RadzenDataGrid<IdentityUserListDto> Grid { get; set; }
    private Paging<List<IdentityUserListDto>> GridData { get; set; } = new();

    private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args)
    {
        if (!args.FirstRender)
        {
            return;
        }

        _ = SetRadzenTexts(args.Grid);
    }

    private async Task SetRadzenTexts(RadzenComponent radzenComponent)
    {
        var parameters = ParameterView.FromDictionary(
            RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object?) l.Value)
        );
        await radzenComponent.SetParametersAsync(parameters);

        await InvokeAsync(StateHasChanged);
    }

    private async Task OpenUpsertModal(IdentityUserListDto? args = null)
    {
        var result = await DialogService.OpenAsync<UpsertApplicationUser>(
            args == null ? "Kullanıcı Ekle" : "Kullanıcı Düzenle - " + args.UserName,
            new Dictionary<string, object?>
            {
                { nameof(UpsertApplicationUser.Id), args?.Id }
            },
            options: new DialogOptions
            {
                Width = "700px",
                CssClass = "mw-100"
            }
        );

        if (result != null)
        {
            await Grid.Reload();
        }
    }

    private async Task DeleteRow(IdentityUserListDto dto)
    {
        if (await DialogService.Confirm(
                "Bu kullanıcıyı silmek istediğinizden emin misiniz?",
                "Kullanıcı Sil",
                new ConfirmOptions
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }
            ) == true)
        {
            var response = await IdentityUserService.DeleteAsync(dto.Id);

            if (response.Ok)
            {
                await Grid.Reload();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
    }

    private void LoadDataDebounce(LoadDataArgs args)
    {
        ComponentDebouncer.Debounce(300, async () => { await LoadGridData(args); });
    }

    private async Task LoadGridData(LoadDataArgs args)
    {
        var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

        var response = await IdentityUserService.GetPagedListAsync(pager);
        if (response.Ok)
        {
            GridData = response.Result;
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }

        await InvokeAsync(StateHasChanged);
    }
}