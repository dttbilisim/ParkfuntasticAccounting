using ecommerce.Admin.Domain.Dtos.CashRegisterMovementDto;
using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class CashRegisterMovements
    {
        [Inject] protected ICashRegisterMovementService MovementService { get; set; } = null!;
        [Inject] protected ICashRegisterService CashRegisterService { get; set; } = null!;
        [Inject] protected ICustomerService CustomerService { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;

        protected List<CashRegisterMovementListDto>? Items { get; set; }
        protected int Count { get; set; }
        protected List<CashRegisterBalanceSummaryDto>? BalanceSummaries { get; set; }
        protected RadzenDataGrid<CashRegisterMovementListDto>? Grid;
        protected int? FilterCashRegisterId { get; set; }
        protected CashRegisterMovementType? FilterMovementType { get; set; }
        protected int? FilterCustomerId { get; set; }
        protected DateTime? FilterStartDate { get; set; }
        protected DateTime? FilterEndDate { get; set; }

        protected List<CashRegisterListDto> CashRegisterList { get; set; } = new();
        protected List<CustomerListDto> CustomerList { get; set; } = new();
        protected bool CashRegistersLoading { get; set; } = true;
        protected bool CustomersLoading { get; set; } = true;

        protected List<SelectItemDto<CashRegisterMovementType?>> MovementTypeOptions { get; set; } = new()
        {
            new SelectItemDto<CashRegisterMovementType?> { Text = "Kasa Girişi", Value = CashRegisterMovementType.In },
            new SelectItemDto<CashRegisterMovementType?> { Text = "Kasa Çıkışı", Value = CashRegisterMovementType.Out }
        };

        /// <summary>Kasaya göre grupla (varsayılan açık).</summary>
        protected bool GroupByKasa { get; set; } = true;

        /// <summary>Mevcut sayfa verisine göre kasa bazlı alt toplamlar (grup başlığında gösterilir).</summary>
        protected IReadOnlyDictionary<string, (decimal TotalIn, decimal TotalOut, decimal Balance)> GroupTotalsByKasa
        {
            get
            {
                if (Items == null || !Items.Any()) return new Dictionary<string, (decimal, decimal, decimal)>();
                return Items
                    .GroupBy(x => x.CashRegisterName)
                    .ToDictionary(
                        g => g.Key,
                        g => (
                            g.Where(x => x.MovementType == CashRegisterMovementType.In).Sum(x => x.Amount),
                            g.Where(x => x.MovementType == CashRegisterMovementType.Out).Sum(x => x.Amount),
                            g.Where(x => x.MovementType == CashRegisterMovementType.In).Sum(x => x.Amount) - g.Where(x => x.MovementType == CashRegisterMovementType.Out).Sum(x => x.Amount)
                        ));
            }
        }

        protected string GetGroupTotalIn(string? kasaName) =>
            kasaName != null && GroupTotalsByKasa.ContainsKey(kasaName) ? GroupTotalsByKasa[kasaName].TotalIn.ToString("N2") : "0,00";

        protected string GetGroupTotalOut(string? kasaName) =>
            kasaName != null && GroupTotalsByKasa.ContainsKey(kasaName) ? GroupTotalsByKasa[kasaName].TotalOut.ToString("N2") : "0,00";

        protected string GetGroupBalance(string? kasaName) =>
            kasaName != null && GroupTotalsByKasa.ContainsKey(kasaName) ? GroupTotalsByKasa[kasaName].Balance.ToString("N2") : "0,00";

        protected string GetGroupSummaryClass(string? kasaName)
        {
            var s = BalanceSummaries?.FirstOrDefault(x => x.CashRegisterName == (kasaName ?? ""));
            return s == null ? "" : s.CurrentBalance >= 0 ? "text-success" : "text-danger";
        }

        protected string GetGroupSummaryText(string? kasaName)
        {
            var s = BalanceSummaries?.FirstOrDefault(x => x.CashRegisterName == (kasaName ?? ""));
            return s == null ? "" : $"Güncel bakiye: {s.CurrentBalance:N2} {s.CurrencyCode}";
        }

        private PageSetting _pager = new();
        private bool _initialLoadDone;

        protected override async Task OnInitializedAsync()
        {
            await LoadCashRegistersAsync();
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

        private async Task LoadCashRegistersAsync()
        {
            CashRegistersLoading = true;
            try
            {
                var res = await CashRegisterService.GetCashRegisters();
                if (res.Ok && res.Result != null)
                    CashRegisterList = res.Result;
                else
                    CashRegisterList = new List<CashRegisterListDto>();
            }
            finally
            {
                CashRegistersLoading = false;
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
                    CustomerList = response.Result.Data;
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

                var response = await MovementService.GetPaged(
                    _pager,
                    cashRegisterId: FilterCashRegisterId,
                    movementType: FilterMovementType,
                    customerId: FilterCustomerId,
                    startDate: FilterStartDate,
                    endDate: FilterEndDate);

                if (response.Ok && response.Result != null)
                {
                    Items = response.Result.Data ?? new List<CashRegisterMovementListDto>();
                    Count = response.Result.DataCount;
                }
                else
                {
                    Items = new List<CashRegisterMovementListDto>();
                    Count = 0;
                    if (!response.Ok)
                        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }

                if (Grid != null)
                {
                    Grid.Groups.Clear();
                    if (GroupByKasa && Items != null && Items.Any())
                        Grid.Groups.Add(new GroupDescriptor { Property = "CashRegisterName", Title = "Kasa" });
                }

                var balanceRes = await MovementService.GetBalanceSummary(
                    cashRegisterId: FilterCashRegisterId,
                    startDate: FilterStartDate,
                    endDate: FilterEndDate);
                BalanceSummaries = balanceRes.Ok && balanceRes.Result != null ? balanceRes.Result : new List<CashRegisterBalanceSummaryDto>();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
                Items = new List<CashRegisterMovementListDto>();
                Count = 0;
                if (Grid != null) Grid.Groups.Clear();
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
            FilterCashRegisterId = null;
            FilterMovementType = null;
            FilterCustomerId = null;
            FilterStartDate = null;
            FilterEndDate = null;
            if (Grid != null)
                await Grid.FirstPage(true);
        }

        protected async Task OpenBalanceModal()
        {
            await DialogService.OpenAsync<Modals.CashRegisterBalanceSummaryModal>("Kasa Bakiyeleri",
                new Dictionary<string, object>
                {
                    { "BalanceSummaries", BalanceSummaries ?? new List<CashRegisterBalanceSummaryDto>() },
                    { "StartDate", FilterStartDate },
                    { "EndDate", FilterEndDate }
                },
                new DialogOptions { Width = "560px", Resizable = true, Draggable = true });
        }

        protected async Task OpenTransferModal()
        {
            var result = await DialogService.OpenAsync<Modals.CashRegisterTransferModal>("Kasalar Arası Virman",
                new Dictionary<string, object> { { "CashRegisterList", CashRegisterList } },
                new DialogOptions { Width = "520px", Resizable = true, Draggable = true });
            if (result == true && Grid != null)
                await Grid.Reload();
        }

        protected async Task AddClick()
        {
            var result = await DialogService.OpenAsync<Modals.UpsertCashRegisterMovement>("Yeni Kasa Hareketi",
                null,
                new DialogOptions { Width = "520px", Resizable = true, Draggable = true });
            if (result == true && Grid != null)
                await Grid.Reload();
        }

        protected async Task OnRowSelect(CashRegisterMovementListDto item)
        {
            await EditRow(item);
        }

        protected async Task EditRow(CashRegisterMovementListDto item)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertCashRegisterMovement>("Kasa Hareketi Düzenle",
                new Dictionary<string, object> { { "Id", item.Id } },
                new DialogOptions { Width = "520px", Resizable = true, Draggable = true });
            if (result == true && Grid != null)
                await Grid.Reload();
        }

        protected async Task DeleteRow(CashRegisterMovementListDto item)
        {
            var confirm = await DialogService.Confirm(
                $"#{item.Id} numaralı kasa hareketini silmek istediğinize emin misiniz?",
                "Silme Onayı",
                new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm != true) return;

            var response = await MovementService.Delete(new AuditWrapDto<CashRegisterMovementDeleteDto>
            {
                UserId = Security.User.Id,
                Dto = new CashRegisterMovementDeleteDto { Id = item.Id }
            });

            if (response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kasa hareketi silindi.");
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
