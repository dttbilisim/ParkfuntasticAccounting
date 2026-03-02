using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Plasiyer.Modals;

public partial class CustomerBalanceModal : ComponentBase
{
    [Inject] protected ICustomerAccountTransactionService TransactionService { get; set; } = default!;
    [Inject] protected NotificationService NotificationService { get; set; } = default!;
    
    [Parameter] public int CustomerId { get; set; }
    
    protected CustomerAccountReportDto? Report { get; set; }
    protected bool IsLoading { get; set; } = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadReport();
    }

    private async Task LoadReport()
    {
        IsLoading = true;
        try
        {
            var result = await TransactionService.GetCustomerAccountReport(CustomerId);
            if (result.Ok)
            {
                Report = result.Result;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage 
                { 
                    Severity = NotificationSeverity.Error, 
                    Summary = "Hata", 
                    Detail = "Cari hesap verileri alınırken bir hata oluştu.",
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
