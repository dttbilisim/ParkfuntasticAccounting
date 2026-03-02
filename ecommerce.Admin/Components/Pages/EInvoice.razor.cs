using ecommerce.Admin.Services;
using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Invoice;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages
{
    public partial class EInvoice
    {
        [Inject] protected IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

        // Tab state
        private int _activeTab = 0;

        // Filtre state
        private DateTime? _startDate = DateTime.Now.AddMonths(-1);
        private DateTime? _endDate = DateTime.Now;
        private string? _searchText;

        // Yükleme state
        private bool _isLoading;
        private bool _isDownloading;
        private bool _isPreviewing;
        private string? _errorMessage;

        // Önizleme modal state
        private bool _showPreviewModal;
        private bool _isPreviewLoading;
        private string? _previewHtml;
        private string? _previewError;
        private string? _previewDocNo;

        // Gelen faturalar
        private List<InboxInvoiceItemDto>? _inboxInvoices;
        private int _inboxTotalCount;

        // Giden faturalar
        private List<OutboxInvoiceItemDto>? _outboxInvoices;
        private int _outboxTotalCount;

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        private async Task SwitchTab(int tab)
        {
            if (_activeTab == tab) return;
            _activeTab = tab;
            _errorMessage = null;
            await LoadData();
        }

        private async Task SearchClick()
        {
            await LoadData();
        }

        private async Task ClearFilters()
        {
            _startDate = DateTime.Now.AddMonths(-1);
            _endDate = DateTime.Now;
            _searchText = null;
            _errorMessage = null;
            await LoadData();
        }

        private async Task LoadData()
        {
            _isLoading = true;
            _errorMessage = null;

            try
            {
                if (_activeTab == 0)
                {
                    await LoadInboxData();
                }
                else
                {
                    await LoadOutboxData();
                }
            }
            catch (Exception ex)
            {
                _errorMessage = $"Veri yüklenirken hata oluştu: {ex.Message}";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async Task LoadInboxData()
        {
            var request = new GetInboxInvoiceRequestDto
            {
                IsDetail = true,
                PageCount = 0,
                PageSize = 100
            };

            var response = await OdaksoftInvoiceService.GetInboxInvoicesAsync(request);

            if (response.Status)
            {
                _inboxInvoices = (response.Data ?? new List<InboxInvoiceItemDto>())
                    .OrderByDescending(x => x.DocDate ?? x.CreateDate)
                    .ToList();
                _inboxTotalCount = response.TotalCount;
            }
            else
            {
                _errorMessage = response.Message ?? response.ExceptionMessage ?? "Gelen faturalar alınamadı.";
                _inboxInvoices = new List<InboxInvoiceItemDto>();
                _inboxTotalCount = 0;
            }
        }

        private async Task LoadOutboxData()
        {
            var request = new GetOutboxInvoiceFilterRequestDto
            {
                IsDetail = true,
                StartDate = _startDate,
                EndDate = _endDate,
                PageCount = 0,
                PageSize = 100
            };

            // Arama metni varsa hem firma adı hem VKN olarak dene
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                // Sadece rakamlardan oluşuyorsa VKN, değilse firma adı
                if (_searchText.Trim().All(char.IsDigit))
                {
                    request.Identifier = _searchText.Trim();
                }
                else
                {
                    request.AccountName = _searchText.Trim();
                }
            }

            var response = await OdaksoftInvoiceService.GetOutboxInvoicesAsync(request);

            if (response.Status)
            {
                _outboxInvoices = (response.Data ?? new List<OutboxInvoiceItemDto>())
                    .OrderByDescending(x => x.DocDate ?? x.CreateDate)
                    .ToList();
                _outboxTotalCount = response.TotalCount;
            }
            else
            {
                _errorMessage = response.Message ?? response.ExceptionMessage ?? "Giden faturalar alınamadı.";
                _outboxInvoices = new List<OutboxInvoiceItemDto>();
                _outboxTotalCount = 0;
            }
        }

        private async Task DownloadOutboxClick(OutboxInvoiceItemDto invoice)
        {
            if (_isDownloading || string.IsNullOrEmpty(invoice.Ettn)) return;

            _isDownloading = true;

            try
            {
                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(invoice.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
                {
                    var fileName = $"Fatura_{invoice.DocNo}_{invoice.Ettn}.pdf";
                    await JSRuntime.InvokeVoidAsync("downloadFileFromBase64", new object?[] { response.ByteArray, fileName, "application/pdf" });

                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = $"{invoice.DocNo} faturası indirildi."
                    });
                }
                else if (response.Status && !string.IsNullOrEmpty(response.Html))
                {
                    await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Fatura indirilemedi",
                        response.Message ?? response.ExceptionMessage ?? "Bilinmeyen hata");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İndirme hatası", ex.Message);
            }
            finally
            {
                _isDownloading = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private static BadgeStyle GetStatusBadgeStyle(string? status)
        {
            return status?.ToUpperInvariant() switch
            {
                "GONDERILDI" or "SENT" => BadgeStyle.Info,
                "ONAYLANDI" or "APPROVED" => BadgeStyle.Success,
                "REDDEDILDI" or "REJECTED" => BadgeStyle.Danger,
                "IPTAL" or "CANCELLED" => BadgeStyle.Danger,
                "TASLAK" or "DRAFT" => BadgeStyle.Warning,
                "YENI" or "NEW" => BadgeStyle.Primary,
                _ => BadgeStyle.Light
            };
        }

        /// <summary>
        /// Giden fatura önizleme butonuna tıklanınca çalışır
        /// </summary>
        private async Task PreviewOutboxClick(OutboxInvoiceItemDto invoice)
        {
            if (_isPreviewing || string.IsNullOrEmpty(invoice.Ettn)) return;

            _isPreviewing = true;
            _showPreviewModal = true;
            _isPreviewLoading = true;
            _previewHtml = null;
            _previewError = null;
            _previewDocNo = invoice.DocNo ?? invoice.Ettn;
            await InvokeAsync(StateHasChanged);

            try
            {
                var response = await OdaksoftInvoiceService.PreviewOutboxInvoiceAsync(invoice.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.Data))
                {
                    _previewHtml = response.Data;
                }
                else
                {
                    _previewError = response.Message ?? "Fatura önizlemesi alınamadı.";
                }
            }
            catch (Exception ex)
            {
                _previewError = $"Önizleme hatası: {ex.Message}";
            }
            finally
            {
                _isPreviewLoading = false;
                _isPreviewing = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Gelen fatura önizleme butonuna tıklanınca çalışır
        /// </summary>
        private async Task PreviewInboxClick(InboxInvoiceItemDto invoice)
        {
            if (_isPreviewing || string.IsNullOrEmpty(invoice.Ettn)) return;

            _isPreviewing = true;
            _showPreviewModal = true;
            _isPreviewLoading = true;
            _previewHtml = null;
            _previewError = null;
            _previewDocNo = invoice.DocNo ?? invoice.Ettn;
            await InvokeAsync(StateHasChanged);

            try
            {
                var response = await OdaksoftInvoiceService.PreviewInboxInvoiceAsync(invoice.Ettn, invoice.RefNo);

                if (response.Status && !string.IsNullOrEmpty(response.Data))
                {
                    _previewHtml = response.Data;
                }
                else
                {
                    _previewError = response.Message ?? "Fatura önizlemesi alınamadı.";
                }
            }
            catch (Exception ex)
            {
                _previewError = $"Önizleme hatası: {ex.Message}";
            }
            finally
            {
                _isPreviewLoading = false;
                _isPreviewing = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// Önizleme modalını kapatır
        /// </summary>
        private void ClosePreview()
        {
            _showPreviewModal = false;
            _previewHtml = null;
            _previewError = null;
            _previewDocNo = null;
        }

        /// <summary>
        /// Önizleme HTML'ini yeni sekmede açar
        /// </summary>
        private async Task OpenPreviewInNewTab()
        {
            if (!string.IsNullOrEmpty(_previewHtml))
            {
                await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { _previewHtml });
            }
        }
    }
}
