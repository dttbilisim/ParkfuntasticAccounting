using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class InvoiceDetailModal
    {
        [Parameter] public int InvoiceId { get; set; }

        [Inject] protected IInvoiceService InvoiceService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected ecommerce.Odaksodt.Abstract.IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; } = null!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

        private InvoiceUpsertDto? selectedInvoice;
        private bool isLoading = true;
        private bool _isDownloading = false;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                isLoading = true;
                var result = await InvoiceService.GetInvoiceById(InvoiceId);

                if (result.Ok && result.Result != null)
                {
                    selectedInvoice = result.Result;
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = result.Metadata?.Message ?? "Fatura detayı yüklenemedi.",
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
                    Detail = $"Fatura detayı yüklenirken bir hata oluştu: {ex.Message}",
                    Duration = 4000
                });
            }
            finally
            {
                isLoading = false;
            }
        }

        /// <summary>
        /// e-Fatura PDF indirme
        /// </summary>
        private async Task DownloadEInvoicePdf()
        {
            if (_isDownloading || selectedInvoice == null || string.IsNullOrEmpty(selectedInvoice.Ettn)) return;

            _isDownloading = true;
            StateHasChanged();

            try
            {
                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(selectedInvoice.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
                {
                    var fileName = $"Fatura_{selectedInvoice.InvoiceNo}_{selectedInvoice.Ettn}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = $"{selectedInvoice.InvoiceNo} faturası indirildi."
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
                _isDownloading = false;
                StateHasChanged();
            }
        }
    }
}
