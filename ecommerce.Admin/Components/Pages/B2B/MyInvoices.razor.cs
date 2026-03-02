using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.B2B
{
    public partial class MyInvoices : IDisposable
    {
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected IInvoiceService InvoiceService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected ecommerce.Odaksodt.Abstract.IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; } = null!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

        private List<InvoiceListDto>? invoices = new();
        private bool isLoading = true;
        private string searchInvoiceNumber = string.Empty;
        private RadzenDataGrid<InvoiceListDto>? invoicesGrid;
        private int? _downloadingInvoiceId;
        
        // Invoice Detail Modal properties removed independently

        protected override async Task OnInitializedAsync()
        {
            try
            {
                if (Security.User == null)
                {
                    NavigationManager.NavigateTo("/login");
                    return;
                }

                // Check for search query parameter
                var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
                var queryParams = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
                if (queryParams.TryGetValue("search", out var searchValue))
                {
                    searchInvoiceNumber = searchValue.ToString().Trim();
                }

                await LoadInvoices();
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Faturalar yüklenirken bir hata oluştu.",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadInvoices()
        {
            try
            {
                isLoading = true;
                StateHasChanged();

                Console.WriteLine($"[MyInvoices] LoadInvoices started - SelectedCustomerId: {Security.SelectedCustomerId}, User.CustomerId: {Security.User?.CustomerId}, User.SalesPersonId: {Security.User?.SalesPersonId}");

                ecommerce.Core.Utils.ResultSet.IActionResult<List<InvoiceListDto>> result;

                if (Security.SelectedCustomerId.HasValue && Security.SelectedCustomerId.Value > 0)
                {
                    // Plasiyer selected a specific customer
                    Console.WriteLine($"[MyInvoices] Calling GetCustomerInvoices({Security.SelectedCustomerId.Value})");
                    result = await InvoiceService.GetCustomerInvoices(Security.SelectedCustomerId.Value);
                }
                else if (Security.User?.SalesPersonId.HasValue == true)
                {
                    // Plasiyer with no customer selected -> Show ALL linked customers' invoices
                    Console.WriteLine($"[MyInvoices] Calling GetPlasiyerCustomersInvoices({Security.User.Id})");
                    result = await InvoiceService.GetPlasiyerCustomersInvoices(Security.User.Id);
                }
                else
                {
                    // Regular CustomerB2B user NOT SUPPORTED yet - GetMyInvoices() doesn't exist
                    // For now, try with User.CustomerId if available (will be fixed with GetMyInvoices)
                    var customerId = Security.User?.CustomerId;
                    
                    Console.WriteLine($"[MyInvoices] CustomerB2B path - CustomerId: {customerId}");
                    
                    if (!customerId.HasValue || customerId.Value <= 0)
                    {
                        Console.WriteLine($"[MyInvoices] ERROR: Invalid CustomerId - showing warning");
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Warning,
                            Summary = "Uyarı",
                            Detail = $"Müşteri bilgisi bulunamadı. Lütfen yöneticinizle iletişime geçin.",
                            Duration = 6000
                        });
                        return;
                    }
                    
                    Console.WriteLine($"[MyInvoices] Calling GetCustomerInvoices({customerId.Value})");
                    result = await InvoiceService.GetCustomerInvoices(customerId.Value);
                }

                Console.WriteLine($"[MyInvoices] Result - Ok: {result.Ok}, Count: {result.Result?.Count ?? 0}");

                if (result.Ok && result.Result != null)
                {
                    invoices = result.Result;
                    Console.WriteLine($"[MyInvoices] Loaded {invoices.Count} invoices");

                    // Apply search filter if exists
                    if (!string.IsNullOrEmpty(searchInvoiceNumber))
                    {
                        invoices = invoices
                            .Where(i => i.InvoiceNo.Contains(searchInvoiceNumber, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        Console.WriteLine($"[MyInvoices] After search filter: {invoices.Count} invoices");
                    }
                }
                else
                {
                    Console.WriteLine($"[MyInvoices] ERROR: {result.Metadata?.Message}");
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Faturalar alınamadı.",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MyInvoices] EXCEPTION: {ex.Message}");
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = $"Faturalar yüklenirken bir hata oluştu: {ex.Message}",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
                Console.WriteLine($"[MyInvoices] LoadInvoices completed");
            }
        }

        private async Task ApplySearch()
        {
            await LoadInvoices();
        }

        private async Task ClearSearch()
        {
            searchInvoiceNumber = string.Empty;
            await LoadInvoices();
        }

        private async Task HandleSearchKeyPress(KeyboardEventArgs e)
        {
            if (e.Key == "Enter")
            {
                await ApplySearch();
            }
        }

        private async Task ViewInvoice(int invoiceId)
        {
            await DialogService.OpenAsync<Modals.InvoiceDetailModal>(
                "Fatura Detayı",
                new Dictionary<string, object> { { "InvoiceId", invoiceId } },
                new DialogOptions { Width = "900px", Height = "auto", CssClass = "invoice-detail-modal" }
            );
        }

        private async Task DownloadInvoice(InvoiceListDto invoice)
        {
            if (_downloadingInvoiceId != null) return;

            if (string.IsNullOrEmpty(invoice.Ettn))
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Warning,
                    Summary = "Uyarı",
                    Detail = "Bu fatura henüz e-Fatura olarak gönderilmemiş, indirilemez.",
                    Duration = 4000
                });
                return;
            }

            _downloadingInvoiceId = invoice.Id;
            StateHasChanged();

            try
            {
                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(invoice.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
                {
                    var fileName = $"Fatura_{invoice.InvoiceNo}_{invoice.Ettn}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = $"{invoice.InvoiceNo} faturası indirildi."
                    });
                }
                else if (response.Status && !string.IsNullOrEmpty(response.Html))
                {
                    await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Fatura indirilemedi", response.Message ?? "Bilinmeyen hata");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İndirme hatası", ex.Message);
            }
            finally
            {
                _downloadingInvoiceId = null;
                StateHasChanged();
            }
        }

        private void ViewEInvoice(int invoiceId)
        {
            // TODO: Implement e-invoice view
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "Bilgi",
                Detail = "e-Fatura görüntüleme yakında eklenecek.",
                Duration = 3000
            });
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
