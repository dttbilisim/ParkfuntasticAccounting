using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Plasiyer.Modals;

public partial class CustomerInvoicesModal : ComponentBase
{
    [Inject] protected IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    
    [Parameter] public int CustomerId { get; set; }
    
    protected List<InvoiceListDto>? Invoices { get; set; }
    protected bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadInvoices();
    }

    private async Task LoadInvoices()
    {
        IsLoading = true;
        try
        {
            var result = await InvoiceService.GetCustomerInvoices(CustomerId);
            if (result.Ok)
            {
                Invoices = result.Result;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = "Fatura verileri alınırken bir hata oluştu.",
                    Duration = 4000 
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage 
            { 
                Severity = NotificationSeverity.Error, 
                Summary = "Sistem Hatası", 
                Detail = "Beklenmedik bir hata oluştu: " + ex.Message,
                Duration = 5000 
            });
        }
        finally
        {
            IsLoading = false;
        }
    }
}
