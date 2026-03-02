using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.B2B;

public partial class CustomerAccountTransactions
{
    [Inject] protected ICustomerAccountTransactionService TransactionService { get; set; } = null!;
    [Inject] protected DialogService DialogService { get; set; } = null!;
    [Inject] protected NotificationService NotificationService { get; set; } = null!;

    protected List<CustomerAccountTransactionListDto>? Transactions { get; set; }
    protected int Count { get; set; }
    protected RadzenDataGrid<CustomerAccountTransactionListDto>? Grid;
    protected DateTime? FilterStartDate { get; set; }
    protected DateTime? FilterEndDate { get; set; }
    protected int? FilterCustomerId { get; set; }
    protected bool GroupByCustomer { get; set; } = true;
    /// <summary>Filtre dropdown: backend'den gelen, hareketi olan cariler.</summary>
    protected List<FilterCustomerItemDto> FilterCustomers { get; set; } = new();
    /// <summary>Cari bazlı alt toplamlar (backend'den gelir).</summary>
    protected List<CustomerSubtotalItemDto> CustomerSubtotals { get; set; } = new();
    private PageSetting _pager = new();
    private bool _initialDataLoadTriggered;

    /// <summary>Sayfa toplamı: Borç (çıkan)</summary>
    protected decimal PageTotalDebit => Transactions?.Sum(t => t.OutgoingAmount) ?? 0;
    /// <summary>Sayfa toplamı: Alacak (giren)</summary>
    protected decimal PageTotalCredit => Transactions?.Sum(t => t.IncomingAmount) ?? 0;
    /// <summary>Cari bazında sayfa içi bakiye özeti</summary>
    protected decimal PageNet => PageTotalDebit - PageTotalCredit;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_initialDataLoadTriggered)
        {
            _initialDataLoadTriggered = true;
            await LoadData(new LoadDataArgs { Skip = 0, Top = 25 });
        }
    }

    protected async Task LoadData(LoadDataArgs args)
    {
        try
        {
            _pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top ?? 25);

            var response = await TransactionService.GetPagedAllCustomerAccountTransactions(
                _pager,
                customerId: FilterCustomerId,
                startDate: FilterStartDate,
                endDate: FilterEndDate);

            if (response.Ok && response.Result != null)
            {
                Transactions = response.Result.Data?.ToList() ?? new List<CustomerAccountTransactionListDto>();
                Count = response.Result.DataCount;
                FilterCustomers = response.Result.FilterCustomers ?? new List<FilterCustomerItemDto>();
                CustomerSubtotals = response.Result.CustomerSubtotals ?? new List<CustomerSubtotalItemDto>();
                if (Grid != null)
                {
                    Grid.Groups.Clear();
                    if (GroupByCustomer && !FilterCustomerId.HasValue && Transactions.Any())
                        Grid.Groups.Add(new GroupDescriptor { Property = "CustomerName", Title = "Cari" });
                }
            }
            else
            {
                Transactions = new List<CustomerAccountTransactionListDto>();
                Count = 0;
                FilterCustomers = new List<FilterCustomerItemDto>();
                CustomerSubtotals = new List<CustomerSubtotalItemDto>();
                if (Grid != null) Grid.Groups.Clear();
                if (!response.Ok)
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(NotificationSeverity.Error, "Hata", ex.Message);
            Transactions = new List<CustomerAccountTransactionListDto>();
            Count = 0;
            FilterCustomers = new List<FilterCustomerItemDto>();
            CustomerSubtotals = new List<CustomerSubtotalItemDto>();
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
        FilterStartDate = null;
        FilterEndDate = null;
        FilterCustomerId = null;
        if (Grid != null)
            await Grid.FirstPage(true);
    }

    protected async Task OnRowSelect(CustomerAccountTransactionListDto transaction)
    {
        await DialogService.OpenAsync<CustomerAccountTransactionDetailModal>(
            "Cari Hareket Detayı",
            new Dictionary<string, object> { { "Transaction", transaction } },
            new DialogOptions
            {
                Width = "600px",
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = true
            });
    }

    protected void RowRender(RowRenderEventArgs<CustomerAccountTransactionListDto> args)
    {
        if (args.Data.TransactionType == CustomerAccountTransactionType.Debit)
            args.Attributes.Add("style", "background-color: rgba(220, 53, 69, 0.06);");
        else
            args.Attributes.Add("style", "background-color: rgba(25, 135, 84, 0.06);");
    }

    protected async Task OpenOrderDetail(int orderId)
    {
        await DialogService.OpenAsync<OrderDetail>(
            "Sipariş Bilgileri",
            new Dictionary<string, object> { { "Id", orderId } },
            new DialogOptions { Width = "1200px", Resizable = true, Draggable = true });
    }

    protected async Task OpenInvoiceDetail(int invoiceId)
    {
        await DialogService.OpenAsync<InvoiceDetailModal>(
            "Fatura Detayı",
            new Dictionary<string, object> { { "InvoiceId", invoiceId } },
            new DialogOptions { Width = "800px", Resizable = true, Draggable = true });
    }
}
