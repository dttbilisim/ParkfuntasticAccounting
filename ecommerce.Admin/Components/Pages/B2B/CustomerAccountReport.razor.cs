using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.B2B
{
    public partial class CustomerAccountReport : IDisposable
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected ICustomerAccountTransactionService CustomerAccountTransactionService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;

        private CustomerAccountReportDto? accountReport;
        private bool isLoading = true;
        private DateTime? startDate = null;
        private DateTime? endDate = null;
        private RadzenDataGrid<CustomerAccountTransactionListDto>? transactionsGrid;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (Security.User == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                await LoadAccountReport();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Rapor yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadAccountReport()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                var customerId = Security.SelectedCustomerId ?? Security.User?.CustomerId;
                if (!customerId.HasValue)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Uyarı",
                        Detail = "Müşteri bilgisi bulunamadı.",
                        Duration = 4000
                    });
                    return;
                }

                var result = await CustomerAccountTransactionService.GetCustomerAccountReport(
                    customerId.Value,
                    startDate,
                    endDate
                );

                if (result.Ok && result.Result != null)
                {
                    accountReport = result.Result;
                    if (accountReport.Transactions != null)
                    {
                        accountReport.Transactions = accountReport.Transactions
                            .OrderBy(x => x.TransactionDate)
                            .ToList();
                    }
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Rapor alınamadı.",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = $"Rapor yüklenirken bir hata oluştu: {ex.Message}",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task ApplyDateFilter()
        {
            await LoadAccountReport();
        }

        private async Task ClearDateFilter()
        {
            startDate = null;
            endDate = null;
            await LoadAccountReport();
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
