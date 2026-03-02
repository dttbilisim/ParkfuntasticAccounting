using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCardDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class BankPage
    {
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] public IBankService BankService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] protected ecommerce.Admin.Domain.Services.IPermissionService PermissionService { get; set; }
        private const string MENU_NAME = "banks";

        protected List<BankListDto> banks = null;
        protected RadzenDataGrid<BankListDto>? bankDataGrid = new();
        private PageSetting bankPager;
        int bankCount;

        protected List<BankParameterListDto> bankParameters = null;
        protected RadzenDataGrid<BankParameterListDto>? bankParameterDataGrid = new();
        private PageSetting bankParameterPager;
        int bankParameterCount;

        protected List<BankCardListDto> bankCards = null;
        protected RadzenDataGrid<BankCardListDto>? bankCardDataGrid = new();
        private PageSetting bankCardPager;
        int bankCardCount;

        protected List<BankCreditCardInstallmentListDto> bankCreditCardInstallments = null;
        protected RadzenDataGrid<BankCreditCardInstallmentListDto>? bankCreditCardInstallmentDataGrid = new();
        private PageSetting bankCreditCardInstallmentPager;
        int bankCreditCardInstallmentCount;

        protected List<BankCreditCardPrefixListDto> bankCreditCardPrefixes = null;
        protected RadzenDataGrid<BankCreditCardPrefixListDto>? bankCreditCardPrefixDataGrid = new();
        private PageSetting bankCreditCardPrefixPager;
        int bankCreditCardPrefixCount;

        private new DialogOptions DialogOptions = new() { Width = "900px" };

        protected async Task AddBankButtonClick(MouseEventArgs args)
        {
            if (!await PermissionService.CanCreate(MENU_NAME)) { NotificationService.Notify(NotificationSeverity.Error, "Oluşturma yetkiniz bulunmamaktadır."); return; }
            await DialogService.OpenAsync<UpsertBankModal>("Banka Ekle", null, DialogOptions);
            await bankDataGrid.Reload();
        }

        protected async Task EditBank(BankListDto args)
        {
            if (!await PermissionService.CanEdit(MENU_NAME)) { NotificationService.Notify(NotificationSeverity.Error, "Düzenleme yetkiniz bulunmamaktadır."); return; }
            await DialogService.OpenAsync<UpsertBankModal>("Banka Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await bankDataGrid.Reload();
        }

        protected async Task DeleteBank(MouseEventArgs args, BankListDto bank)
        {
            if (!await PermissionService.CanDelete(MENU_NAME)) { NotificationService.Notify(NotificationSeverity.Error, "Silme yetkiniz bulunmamaktadır."); return; }
            if (await DialogService.Confirm("Seçilen banka silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await BankService.DeleteBank(new AuditWrapDto<BankDeleteDto>()
                { UserId = Security.User.Id, Dto = new BankDeleteDto() { Id = bank.Id } });
                if (deleteResult != null)
                {
                    await bankDataGrid.Reload();
                }
            }
        }

        private async Task LoadBankData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            bankPager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            
            if (!await PermissionService.CanView(MENU_NAME))
            {
                 NotificationService.Notify(NotificationSeverity.Error, "Görüntüleme yetkiniz bulunmamaktadır.");
                 return;
            }

            var response = await BankService.GetBanks(bankPager);
            if (response.Ok && response.Result?.Data != null)
            {
                banks = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                bankCount = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        protected async Task AddBankParameterButtonClick(MouseEventArgs args)
        {
            var result = await DialogService.OpenAsync<UpsertBankParameterModal>("Banka Parametresi Ekle", null, DialogOptions);
            if (result == true)
            {
                await ReloadBankParameterGrid();
            }
        }

        protected async Task EditBankParameter(BankParameterListDto args)
        {
            var result = await DialogService.OpenAsync<UpsertBankParameterModal>("Banka Parametresi Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            if (result == true)
            {
                await ReloadBankParameterGrid();
            }
        }

        private async Task ReloadBankParameterGrid()
        {
            var args = new LoadDataArgs { Skip = bankParameterPager?.Skip ?? 0, Top = bankParameterPager?.Take ?? 20, OrderBy = bankParameterPager?.OrderBy ?? "Id desc", Filter = bankParameterPager?.Filter ?? "" };
            await LoadBankParameterData(args);
            if (bankParameterDataGrid != null)
                await bankParameterDataGrid.Reload();
            StateHasChanged();
        }

        protected async Task DeleteBankParameter(MouseEventArgs args, BankParameterListDto bankParameter)
        {
            if (await DialogService.Confirm("Seçilen banka parametresi silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await BankService.DeleteBankParameter(new AuditWrapDto<BankParameterDeleteDto>()
                { UserId = Security.User.Id, Dto = new BankParameterDeleteDto() { Id = bankParameter.Id } });
                if (deleteResult != null)
                {
                    await ReloadBankParameterGrid();
                }
            }
        }

        private async Task LoadBankParameterData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            bankParameterPager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            var response = await BankService.GetBankParameters(bankParameterPager);
            if (response.Ok && response.Result?.Data != null)
            {
                bankParameters = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                bankParameterCount = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        protected async Task AddBankCardButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertBankCardModal>("Banka Kartı Ekle", null, DialogOptions);
            await bankCardDataGrid.Reload();
        }

        protected async Task EditBankCard(BankCardListDto args)
        {
            await DialogService.OpenAsync<UpsertBankCardModal>("Banka Kartı Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await bankCardDataGrid.Reload();
        }

        protected async Task DeleteBankCard(MouseEventArgs args, BankCardListDto bankCard)
        {
            if (await DialogService.Confirm("Seçilen banka kartı silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await BankService.DeleteBankCard(new AuditWrapDto<BankCardDeleteDto>()
                { UserId = Security.User.Id, Dto = new BankCardDeleteDto() { Id = bankCard.Id } });
                if (deleteResult != null)
                {
                    await bankCardDataGrid.Reload();
                }
            }
        }

        private async Task LoadBankCardData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            bankCardPager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            var response = await BankService.GetBankCards(bankCardPager);
            if (response.Ok && response.Result?.Data != null)
            {
                bankCards = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                bankCardCount = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        protected async Task AddBankCreditCardInstallmentButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertBankCreditCardInstallmentModal>("Taksit Ekle", null, DialogOptions);
            await bankCreditCardInstallmentDataGrid.Reload();
        }

        protected async Task EditBankCreditCardInstallment(BankCreditCardInstallmentListDto args)
        {
            await DialogService.OpenAsync<UpsertBankCreditCardInstallmentModal>("Taksit Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await bankCreditCardInstallmentDataGrid.Reload();
        }

        protected async Task DeleteBankCreditCardInstallment(MouseEventArgs args, BankCreditCardInstallmentListDto installment)
        {
            if (await DialogService.Confirm("Seçilen taksit bilgisi silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await BankService.DeleteBankCreditCardInstallment(new AuditWrapDto<BankCreditCardInstallmentDeleteDto>()
                { UserId = Security.User.Id, Dto = new BankCreditCardInstallmentDeleteDto() { Id = installment.Id } });
                if (deleteResult != null)
                {
                    await bankCreditCardInstallmentDataGrid.Reload();
                }
            }
        }

        private async Task LoadBankCreditCardInstallmentData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            bankCreditCardInstallmentPager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            var response = await BankService.GetBankCreditCardInstallments(bankCreditCardInstallmentPager);
            if (response.Ok && response.Result?.Data != null)
            {
                bankCreditCardInstallments = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                bankCreditCardInstallmentCount = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        protected async Task AddBankCreditCardPrefixButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertBankCreditCardPrefixModal>("Kart Prefix Ekle", null, DialogOptions);
            await bankCreditCardPrefixDataGrid.Reload();
        }

        protected async Task EditBankCreditCardPrefix(BankCreditCardPrefixListDto args)
        {
            await DialogService.OpenAsync<UpsertBankCreditCardPrefixModal>("Kart Prefix Düzenle", new Dictionary<string, object> { { "Id", args.Id } }, DialogOptions);
            await bankCreditCardPrefixDataGrid.Reload();
        }

        protected async Task DeleteBankCreditCardPrefix(MouseEventArgs args, BankCreditCardPrefixListDto prefix)
        {
            if (await DialogService.Confirm("Seçilen kart prefix bilgisi silinecek. Onaylıyor musunuz?", "Kayıt Sil",
                    new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
            {
                var deleteResult = await BankService.DeleteBankCreditCardPrefix(new AuditWrapDto<BankCreditCardPrefixDeleteDto>()
                { UserId = Security.User.Id, Dto = new BankCreditCardPrefixDeleteDto() { Id = prefix.Id } });
                if (deleteResult != null)
                {
                    await bankCreditCardPrefixDataGrid.Reload();
                }
            }
        }

        private async Task LoadBankCreditCardPrefixData(LoadDataArgs args)
        {
            var orderfilter = (args.OrderBy ?? string.Empty).Replace("np", "");
            orderfilter = orderfilter == string.Empty ? "Id desc" : orderfilter;
            var filter = (args.Filter ?? string.Empty).Replace("np", "");
            bankCreditCardPrefixPager = new PageSetting(filter, orderfilter, args.Skip, args.Top);
            var response = await BankService.GetBankCreditCardPrefixes(bankCreditCardPrefixPager);
            if (response.Ok && response.Result?.Data != null)
            {
                bankCreditCardPrefixes = response.Result.Data.OrderByDescending(x => x.Id).ToList();
                bankCreditCardPrefixCount = response.Result.DataCount;
            }
            else if (response.Exception != null)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
    }
}
