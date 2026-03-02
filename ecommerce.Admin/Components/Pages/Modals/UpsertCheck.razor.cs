using ecommerce.Admin.Domain.Dtos.CheckDto;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCheck
    {
        [Parameter] public int? Id { get; set; }

        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public ICheckService CheckService { get; set; } = null!;
        [Inject] public IBankBranchService BankBranchService { get; set; } = null!;
        [Inject] public IBankService BankService { get; set; } = null!;
        [Inject] public ICustomerService CustomerService { get; set; } = null!;
        [Inject] public ICurrencyAdminService CurrencyService { get; set; } = null!;

        protected CheckUpsertDto? Model { get; set; }
        protected bool Saving { get; set; }

        protected List<BankListDto> BankList { get; set; } = new();
        protected List<CustomerListDto> CustomerList { get; set; } = new();
        protected List<BankBranchListDto> BankBranchList { get; set; } = new();
        protected List<BankBranchListDto> FilteredBankBranchList => Model?.BankId > 0
            ? BankBranchList.Where(x => x.BankId == Model.BankId).ToList()
            : new List<BankBranchListDto>();
        protected List<SelectItemDto<int?>> CurrencyOptions { get; set; } = new();
        protected List<SelectItemDto<CheckStatus>> CheckStatusOptions { get; set; } = new()
        {
            new SelectItemDto<CheckStatus> { Text = "Portföyde", Value = CheckStatus.InPortfolio },
            new SelectItemDto<CheckStatus> { Text = "Tahsil Edildi", Value = CheckStatus.Collected },
            new SelectItemDto<CheckStatus> { Text = "Reddedildi", Value = CheckStatus.Bounced },
            new SelectItemDto<CheckStatus> { Text = "İade", Value = CheckStatus.Returned }
        };

        protected override async Task OnInitializedAsync()
        {
            await LoadDropdownsAsync();

            if (Id.HasValue && Id.Value > 0)
            {
                var response = await CheckService.GetById(Id.Value);
                if (response.Ok && response.Result != null)
                    Model = response.Result;
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                    DialogService.Close(false);
                }
            }
            else
            {
                Model = new CheckUpsertDto
                {
                    DueDate = DateTime.Now.AddMonths(1),
                    CheckStatus = CheckStatus.InPortfolio
                };
            }
        }

        private async Task LoadDropdownsAsync()
        {
            var bankPager = new PageSetting(null, null, 0, 500);
            var bankRes = await BankService.GetBanks(bankPager);
            if (bankRes.Ok && bankRes.Result?.Data != null)
                BankList = bankRes.Result.Data;

            var customerPager = new PageSetting(null, null, 0, 500);
            var customerRes = await CustomerService.GetPagedCustomers(customerPager);
            if (customerRes.Ok && customerRes.Result?.Data != null)
                CustomerList = customerRes.Result.Data;

            var branchRes = await BankBranchService.GetList();
            if (branchRes.Ok && branchRes.Result != null)
                BankBranchList = branchRes.Result;

            var currencyRes = await CurrencyService.GetCurrencies();
            if (currencyRes.Ok && currencyRes.Result != null)
            {
                var currencies = currencyRes.Result
                    .GroupBy(c => c.CurrencyCode)
                    .Select(g => g.OrderByDescending(c => c.CreatedDate).First())
                    .ToList();
                CurrencyOptions = currencies
                    .Select(c => new SelectItemDto<int?> { Text = $"{c.CurrencyCode} - {c.CurrencyName}", Value = c.Id })
                    .OrderBy(x => currencies.FirstOrDefault(c => c.Id == x.Value)?.CurrencyCode != "TRY")
                    .ThenBy(x => x.Text)
                    .ToList();
            }
        }

        protected async Task OnBankChange(object? _)
        {
            if (Model != null && Model.BankId == 0)
                Model.BankBranchId = null;
            else if (Model != null && !FilteredBankBranchList.Any(x => x.Id == Model.BankBranchId))
                Model.BankBranchId = null;
            await InvokeAsync(StateHasChanged);
        }

        protected async Task FormSubmit()
        {
            if (Model == null) return;
            try
            {
                Saving = true;

                if (Model.Id.HasValue && Model.Id.Value > 0)
                {
                    var response = await CheckService.Update(new AuditWrapDto<CheckUpsertDto>
                    {
                        UserId = Security.User.Id,
                        Dto = Model
                    });
                    if (response.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Çek kaydı güncellendi.");
                        DialogService.Close(true);
                    }
                    else
                        NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
                else
                {
                    var response = await CheckService.Create(new AuditWrapDto<CheckUpsertDto>
                    {
                        UserId = Security.User.Id,
                        Dto = Model
                    });
                    if (response.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Çek kaydı oluşturuldu.");
                        DialogService.Close(true);
                    }
                    else
                        NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            }
            finally
            {
                Saving = false;
            }
        }
    }
}
