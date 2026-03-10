using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using ecommerce.Admin.Components.Pages.Plasiyer.Modals;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Components.Pages.Plasiyer;

public partial class PlasiyerCustomers : ComponentBase
{
    [Inject] protected ISalesPersonService SalesPersonService { get; set; } = default!;
    [Inject] protected AuthenticationService Security { get; set; } = default!;
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    [Inject] protected DialogService DialogService { get; set; } = default!;
    [Inject] protected TooltipService TooltipService { get; set; } = default!;

    protected void ShowTooltip(ElementReference element, string text, TooltipOptions options = null) => TooltipService.Open(element, text, options);

    protected List<CustomerListDto> customers = new();
    protected bool isLoading = false;
    protected RadzenDataGrid<CustomerListDto> grid = default!;
    protected string searchTerm = string.Empty;

    protected override void OnInitialized()
    {
        if (Security.User?.SalesPersonId == null)
        {
            NavigationManager.NavigateTo("/");
        }
    }

    protected async Task OnSearchKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await SearchCustomers();
    }

    protected async Task SearchCustomers() => await LoadCustomers();

    protected async Task LoadCustomers()
    {
        if (Security.User?.SalesPersonId == null) return;

        var term = searchTerm?.Trim();
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
        {
            customers = new List<CustomerListDto>();
            NotificationService.Notify(NotificationSeverity.Info, "Arama", "Carileri listelemek için en az 2 karakter girin.");
            return;
        }

        isLoading = true;
        try
        {
            var response = await SalesPersonService.GetCustomersOfSalesPerson(Security.User!.SalesPersonId!.Value, term);
            if (response.Ok)
            {
                customers = response.Result ?? new List<CustomerListDto>();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Hata", "Müşteri listesi yüklenemedi.");
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    protected async Task SelectCustomer(CustomerListDto customer)
    {
        await Security.SetSelectedCustomer(customer.Id, customer.Name);
        NotificationService.Notify(NotificationSeverity.Success, "Cari Seçildi", $"{customer.Name} adına işlem yapılıyor.");
        // returnUrl varsa oraya git (örn. checkout'tan geldiyse), yoksa product-search
        var uri = new Uri(NavigationManager.Uri);
        var returnUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out var val) ? val.ToString() : null;
        NavigationManager.NavigateTo(!string.IsNullOrWhiteSpace(returnUrl) ? returnUrl : "/product-search", forceLoad: true);
    }

    protected async Task ViewOrders(CustomerListDto customer)
    {
        await Security.SetSelectedCustomer(customer.Id, customer.Name);
        NotificationService.Notify(NotificationSeverity.Info, "Yönlendiriliyor", $"{customer.Name} siparişlerine gidiliyor...");
        NavigationManager.NavigateTo("/b2b/my-orders");
    }

    protected async Task ViewInvoices(CustomerListDto customer)
    {
        await Security.SetSelectedCustomer(customer.Id, customer.Name);
        NotificationService.Notify(NotificationSeverity.Info, "Yönlendiriliyor", $"{customer.Name} faturalarına gidiliyor...");
        NavigationManager.NavigateTo("/b2b/my-invoices");
    }

    protected async Task ViewBalance(CustomerListDto customer)
    {
        await Security.SetSelectedCustomer(customer.Id, customer.Name);
        NotificationService.Notify(NotificationSeverity.Info, "Yönlendiriliyor", $"{customer.Name} ekstresine gidiliyor...");
        NavigationManager.NavigateTo("/b2b/customer-account-report");
    }

    protected void OnRowRender(RowRenderEventArgs<CustomerListDto> args)
    {
        if (Security.SelectedCustomerId == args.Data.Id)
        {
            args.Attributes.Add("class", "selected-customer-row");
        }
    }
}
