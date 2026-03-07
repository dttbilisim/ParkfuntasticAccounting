using ecommerce.Admin.Domain.Dtos.CheckDto;
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
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Checks
    {
        [Inject] protected ICheckService CheckService { get; set; } = null!;
        [Inject] protected IBankBranchService BankBranchService { get; set; } = null!;
        [Inject] protected IBankService BankService { get; set; } = null!;
        [Inject] protected ICustomerService CustomerService { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;

        protected List<CheckListDto>? Items { get; set; }
        protected int Count { get; set; }
        protected RadzenDataGrid<CheckListDto>? Grid;

        protected int? FilterBankId { get; set; }
        protected int? FilterCustomerId { get; set; }
        protected CheckStatus? FilterCheckStatus { get; set; }
        protected DateTime? FilterDueDateStart { get; set; }
        protected DateTime? FilterDueDateEnd { get; set; }

        protected List<BankListDto> BankList { get; set; } = new();
        protected List<CustomerListDto> CustomerList { get; set; } = new();
        protected bool BanksLoading { get; set; } = true;
        protected bool CustomersLoading { get; set; } = true;

        protected List<SelectItemDto<CheckStatus?>> CheckStatusOptions { get; set; } = new()
        {
            new SelectItemDto<CheckStatus?> { Text = "Portföyde", Value = CheckStatus.InPortfolio },
            new SelectItemDto<CheckStatus?> { Text = "Tahsil Edildi", Value = CheckStatus.Collected },
            new SelectItemDto<CheckStatus?> { Text = "Reddedildi", Value = CheckStatus.Bounced },
            new SelectItemDto<CheckStatus?> { Text = "İade", Value = CheckStatus.Returned }
        };

        private PageSetting _pager = new();
        private bool _initialLoadDone;

        protected override async Task OnInitializedAsync()
        {
            await LoadBanksAsync();
            await LoadCustomersAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!_initialLoadDone)
            {
                _initialLoadDone = true;
                await LoadData(new LoadDataArgs { Skip = 0, Top = 25 });
            }
        }

        private async Task LoadBanksAsync()
        {
            BanksLoading = true;
            try
            {
                var pager = new PageSetting(null, null, 0, 500);
                var res = await BankService.GetBanks(pager);
                if (res.Ok && res.Result?.Data != null)
                    BankList = res.Result.Data;
                else
                    BankList = new List<BankListDto>();
            }
            finally
            {
                BanksLoading = false;
            }
        }

        private async Task LoadCustomersAsync()
        {
            CustomersLoading = true;
            try
            {
                var pager = new PageSetting(null, null, 0, 500);
                var response = await CustomerService.GetPagedCustomers(pager);
                if (response.Ok && response.Result?.Data != null)
                    CustomerList = response.Result.Data.DistinctBy(c => c.Id).ToList();
                else
                    CustomerList = new List<CustomerListDto>();
            }
            finally
            {
                CustomersLoading = false;
            }
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            try
            {
                _pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top ?? 25);

                var response = await CheckService.GetPaged(
                    _pager,
                    bankId: FilterBankId,
                    customerId: FilterCustomerId,
                    checkStatus: FilterCheckStatus,
                    dueDateStart: FilterDueDateStart,
                    dueDateEnd: FilterDueDateEnd);

                if (response.Ok && response.Result != null)
                {
                    Items = response.Result.Data ?? new List<CheckListDto>();
                    Count = response.Result.DataCount;
                }
                else
                {
                    Items = new List<CheckListDto>();
                    Count = 0;
                    if (!response.Ok)
                        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
                Items = new List<CheckListDto>();
                Count = 0;
            }

            await InvokeAsync(StateHasChanged);
        }

        protected async Task ApplyFilter()
        {
            if (Grid != null)
                await Grid.FirstPage(true);
        }

        protected async Task ClearFilter()
        {
            FilterBankId = null;
            FilterCustomerId = null;
            FilterCheckStatus = null;
            FilterDueDateStart = null;
            FilterDueDateEnd = null;
            if (Grid != null)
                await Grid.FirstPage(true);
        }

        protected string GetCheckStatusBadgeClass(CheckStatus status)
        {
            return status switch
            {
                CheckStatus.InPortfolio => "bg-primary",
                CheckStatus.Collected => "bg-success",
                CheckStatus.Bounced => "bg-danger",
                CheckStatus.Returned => "bg-warning text-dark",
                _ => "bg-secondary"
            };
        }

        protected async Task AddClick()
        {
            var result = await DialogService.OpenAsync<Modals.UpsertCheck>("Yeni Çek",
                null,
                new DialogOptions { Width = "560px", Resizable = true, Draggable = true });
            if (result == true && Grid != null)
                await Grid.Reload();
        }

        protected async Task OnRowSelect(CheckListDto item)
        {
            await EditRow(item);
        }

        protected async Task EditRow(CheckListDto item)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertCheck>("Çek Düzenle",
                new Dictionary<string, object> { { "Id", item.Id } },
                new DialogOptions { Width = "560px", Resizable = true, Draggable = true });
            if (result == true && Grid != null)
                await Grid.Reload();
        }

        protected async Task DeleteRow(CheckListDto item)
        {
            var confirm = await DialogService.Confirm(
                $"#{item.Id} — {item.CheckNumber} numaralı çek kaydını silmek istediğinize emin misiniz?",
                "Silme Onayı",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm != true) return;

            var response = await CheckService.Delete(new AuditWrapDto<CheckDeleteDto>
            {
                UserId = Security.User.Id,
                Dto = new CheckDeleteDto { Id = item.Id }
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Çek kaydı silindi.");
                await LoadData(new LoadDataArgs { Skip = _pager.Skip ?? 0, Top = _pager.Take ?? 25, OrderBy = _pager.OrderBy, Filter = _pager.Filter });
                if (Grid != null)
                    await Grid.Reload();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
            }
        }
    }
}
