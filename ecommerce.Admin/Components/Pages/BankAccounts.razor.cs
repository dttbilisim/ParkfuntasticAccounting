using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class BankAccounts
{
    [Inject] protected IBankAccountDefinitionService BankAccountService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;
    [Inject] protected AuthenticationService AuthenticationService { get; set; } = null!;

    protected RadzenDataGrid<BankAccountListDto>? bankAccountGrid;
    protected List<BankAccountListDto>? bankAccounts;
    protected int bankAccountCount;
    protected PageSetting pager = new();

    protected async Task LoadBankAccountData(LoadDataArgs args)
    {
        try
        {
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

            var result = await BankAccountService.GetBankAccounts(pager);
            if (result.Ok && result.Result != null)
            {
                bankAccounts = result.Result.Data?.ToList();
                bankAccountCount = result.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
            }

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }

    protected async Task AddBankAccountClick()
    {
        await OpenBankAccountModal(0);
    }

    protected async Task EditBankAccount(BankAccountListDto item)
    {
        await OpenBankAccountModal(item.Id);
    }

    private async Task OpenBankAccountModal(int id)
    {
        var result = await DialogService.OpenAsync<Modals.UpsertBankAccountModal>(
            id == 0 ? "Yeni Banka Hesabı" : "Banka Hesabı Düzenle",
            new Dictionary<string, object?> { { "Id", id } },
            new DialogOptions
            {
                Width = "1200px",
                Height = "90vh",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = false
            });

        if (result != null && bankAccountGrid != null)
        {
            await bankAccountGrid.Reload();
        }
    }

    protected async Task DeleteBankAccountAsync(Microsoft.AspNetCore.Components.Web.MouseEventArgs args, BankAccountListDto item)
    {
        try
        {
            var confirm = await DialogService.Confirm(
                $"'{item.AccountName}' hesabını silmek istediğinizden emin misiniz?",
                "Banka Hesabı Sil",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

            if (confirm == true)
            {
                var audit = new AuditWrapDto<BankAccountDeleteDto>
                {
                    UserId = AuthenticationService.User.Id,
                    Dto = new BankAccountDeleteDto { Id = item.Id }
                };

                var result = await BankAccountService.DeleteBankAccount(audit);

                if (result != null && result.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kayıt silindi.");
                    if (bankAccountGrid != null)
                    {
                        await bankAccountGrid.Reload();
                    }
                }
                else if (result != null)
                {
                    NotificationService.Notify(NotificationSeverity.Error, result.GetMetadataMessages());
                }
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
        }
    }
}


