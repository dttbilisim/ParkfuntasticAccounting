using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class Customers : ComponentBase
{
    [Inject] private ICustomerService CustomerService { get; set; } = default!;
    [Inject] private DialogService DialogService { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;

    private RadzenDataGrid<CustomerListDto> grid = default!;
    private IEnumerable<CustomerListDto> CustomersList;
    private int Count;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    private async Task LoadData(LoadDataArgs args)
    {
        try
        {
            var pager = new PageSetting
            {
                Skip = args.Skip,
                Take = args.Top,
                OrderBy = args.OrderBy,
                Filter = args.Filter
            };

            var result = await CustomerService.GetPagedCustomers(pager);
            if (result.Ok)
            {
                CustomersList = result.Result!.Data!;
                Count = result.Result!.DataCount;
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Veriler yüklenemedi."
                });
            }
        }
        catch (Exception ex)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Beklenmeyen bir hata oluştu."
            });
        }
        finally
        {
        }
    }

    private async Task OpenUpsertModal(int? id = null)
    {
        var result = await DialogService.OpenAsync<UpsertCustomer>(
            null,
            new Dictionary<string, object> { { "Id", id } },
            new DialogOptions 
            { 
                Width = "850px", 
                Height = "auto", 
                Style = "max-height: 90vh;", 
                ShowTitle = false, 
                ShowClose = false,
                Resizable = true, 
                Draggable = true 
            }
        );

        if (result == true)
        {
            await grid.Reload();
        }
    }

    private async Task DeleteCustomer(CustomerListDto customer)
    {
        var confirm = await DialogService.Confirm("Bu cariyi silmek istediğinize emin misiniz?", "Silme Onayı", 
            new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

        if (confirm == true)
        {
            var result = await CustomerService.DeleteCustomer(customer.Id);
            if (result.Ok)
            {
                 NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Cari silindi."
                });
                await grid.Reload();
            }
            else
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = result.Metadata?.Message ?? "Silme işlemi başarısız."
                });
            }
        }
    }
}
