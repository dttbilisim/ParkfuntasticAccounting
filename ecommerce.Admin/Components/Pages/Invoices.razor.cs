using System.Net.Http;
using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Domain.Dtos.InvoiceTypeDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Invoice;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    /// <summary>
    /// e-Fatura gönderim adımlarını temsil eden model
    /// </summary>
    public class EInvoiceSendStep
    {
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "hourglass_empty";
        public EInvoiceStepStatus Status { get; set; } = EInvoiceStepStatus.Pending;
        public string? ErrorMessage { get; set; }
    }

    public enum EInvoiceStepStatus
    {
        Pending,    // Bekliyor
        InProgress, // İşleniyor
        Success,    // Başarılı
        Error       // Hata
    }

    public partial class Invoices
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = null!;
        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected TooltipService TooltipService { get; set; } = null!;
        [Inject] protected ContextMenuService ContextMenuService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] public IInvoiceService InvoiceService { get; set; } = null!;
        [Inject] public IInvoiceTypeService InvoiceTypeService { get; set; } = null!;
        [Inject] public IOrderService OrderService { get; set; } = null!;
        [Inject] public IOdaksoftInvoiceService OdaksoftInvoiceService { get; set; } = null!;
        [Inject] public IOdaksoftAuthService OdaksoftAuthService { get; set; } = null!;
        [Inject] public ICustomerService CustomerService { get; set; } = null!;
        #endregion

        int count;
        protected List<InvoiceListDto>? invoices;
        protected RadzenDataGrid<InvoiceListDto>? radzenDataGrid = new();
        private PageSetting pager;

        // Filtre: Fatura tipi
        protected int? FilterInvoiceTypeId { get; set; }
        protected IEnumerable<InvoiceTypeListDto> InvoiceTypes { get; set; } = new List<InvoiceTypeListDto>();
        private bool _invoiceTypesLoading;

        // e-Fatura gönderim state
        private int? _sendingInvoiceId;
        private int? _cancellingInvoiceId;
        private List<EInvoiceSendStep> _sendSteps = new();
        private bool _showSendProgress;
        private string? _sendResultEttn;

        protected override async Task OnInitializedAsync()
        {
            await LoadInvoiceTypesAsync();
        }

        private async Task LoadInvoiceTypesAsync()
        {
            _invoiceTypesLoading = true;
            try
            {
                var res = await InvoiceTypeService.GetInvoiceTypesForInvoice();
                if (res.Ok && res.Result != null)
                    InvoiceTypes = res.Result;
                else
                    InvoiceTypes = new List<InvoiceTypeListDto>();
            }
            finally
            {
                _invoiceTypesLoading = false;
            }
        }

        protected async Task LoadData(LoadDataArgs args)
        {
            try
            {
                pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

                var response = await InvoiceService.GetInvoices(pager, FilterInvoiceTypeId);
                if (response.Ok && response.Result != null)
                {
                    invoices = response.Result.Data?.ToList();
                    count = response.Result.DataCount;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }

                await InvokeAsync(StateHasChanged);
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected async Task ApplyFilter()
        {
            if (radzenDataGrid != null)
                await radzenDataGrid.Reload();
        }

        protected async Task ClearFilter()
        {
            FilterInvoiceTypeId = null;
            if (radzenDataGrid != null)
                await radzenDataGrid.Reload();
        }

        protected async Task AddButtonClick()
        {
            await OpenUpsertDialog(null);
        }

        protected async Task AddOrderLinkedInvoiceClick()
        {
            var result = await DialogService.OpenAsync<Modals.UnfacturedOrdersModal>(
                "Faturalanmamış Siparişler",
                new Dictionary<string, object>(),
                new DialogOptions
                {
                    Width = "950px",
                    Height = "650px",
                    Resizable = false,
                    Draggable = true,
                    CloseDialogOnOverlayClick = true,
                    CssClass = "premium-centered-modal" 
                });

            if (result != null)
            {
                if (result is List<int> selectedOrderIds && selectedOrderIds.Any())
                {
                     await OpenUpsertDialogFromOrders(selectedOrderIds);
                }
                else if (result is int selectedOrderId)
                {
                    await OpenUpsertDialogFromOrders(new List<int> { selectedOrderId });
                }
            }
        }

        protected async Task OpenUpsertDialogFromOrders(List<int> orderIds)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertInvoice>(
                "Sipariş Bağlı Fatura Oluştur",
                new Dictionary<string, object> { { "Id", null }, { "OrderIds", orderIds } },
                new DialogOptions
                {
                    Width = "1200px",
                    Height = "800px",
                    Resizable = true,
                    Draggable = true,
                    CloseDialogOnOverlayClick = false
                });

            if (result != null && radzenDataGrid != null)
            {
                await radzenDataGrid.Reload();
            }
        }

        protected async Task OpenUpsertDialogFromOrder(int orderId)
        {
             await OpenUpsertDialogFromOrders(new List<int> { orderId });
        }

        protected async Task EditRow(InvoiceListDto invoice)
        {
            await OpenUpsertDialog(invoice.Id);
        }

        protected async Task OpenUpsertDialog(int? id = null)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertInvoice>(
                id.HasValue ? "Fatura Düzenle" : "Yeni Fatura",
                new Dictionary<string, object> { { "Id", id } },
                new DialogOptions
                {
                    Width = "1200px",
                    Height = "800px",
                    Resizable = true,
                    Draggable = true,
                    CloseDialogOnOverlayClick = false
                });

            if (result != null && radzenDataGrid != null)
            {
                await radzenDataGrid.Reload();
            }
        }

        protected async Task GridDeleteButtonClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs args, InvoiceListDto invoice)
        {
            try
            {
                var confirm = await DialogService.Confirm(
                    $"{invoice.InvoiceNo} numaralı faturayı silmek istediğinizden emin misiniz?",
                    "Fatura Sil",
                    new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });

                if (confirm == true)
                {
                    var response = await InvoiceService.DeleteInvoice(new AuditWrapDto<InvoiceDeleteDto>
                    {
                        UserId = Security.User.Id,
                        Dto = new InvoiceDeleteDto { Id = invoice.Id }
                    });

                    if (response.Ok)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = "Fatura silindi."
                        });
                        
                        if (radzenDataGrid != null)
                        {
                            await radzenDataGrid.Reload();
                        }
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected void RowRender(RowRenderEventArgs<InvoiceListDto> args)
        {
            // e-Fatura gönderilmişse hafif yeşil, iptal edilmişse hafif kırmızı
            var durum = args.Data.EInvoiceStatus?.ToUpperInvariant();
            if (!string.IsNullOrEmpty(args.Data.Ettn) && durum == "GONDERILDI")
            {
                args.Attributes.Add("style", "background-color: rgba(40, 167, 69, 0.08);");
            }
            else if (durum == "IPTAL" || durum == "REDDEDILDI")
            {
                args.Attributes.Add("style", "background-color: rgba(220, 53, 69, 0.08);");
            }
        }

        /// <summary>
        /// Adım adım ilerleme panelini kapatır
        /// </summary>
        protected void CloseSendProgress()
        {
            _showSendProgress = false;
            _sendSteps.Clear();
            _sendResultEttn = null;
        }

        /// <summary>
        /// Adımı günceller ve UI'ı yeniler
        /// </summary>
        private async Task UpdateStep(int index, EInvoiceStepStatus status, string? errorMessage = null)
        {
            if (index < _sendSteps.Count)
            {
                _sendSteps[index].Status = status;
                _sendSteps[index].ErrorMessage = errorMessage;
                _sendSteps[index].Icon = status switch
                {
                    EInvoiceStepStatus.InProgress => "sync",
                    EInvoiceStepStatus.Success => "check_circle",
                    EInvoiceStepStatus.Error => "error",
                    _ => "hourglass_empty"
                };
            }
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// e-Fatura gönder butonuna tıklanınca çalışır.
        /// Adım adım ilerleme gösterir, mükerrer işlem koruması sağlar.
        /// </summary>
        protected async Task SendEInvoiceClick(InvoiceListDto invoice)
        {
            // Mükerrer işlem koruması - zaten bir gönderim devam ediyorsa çık
            if (_sendingInvoiceId != null) return;

            // Zaten gönderilmişse ve iptal edilmemişse uyar
            if (!string.IsNullOrEmpty(invoice.Ettn) && invoice.EInvoiceStatus?.ToUpperInvariant() != "IPTAL")
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Bu fatura zaten e-Fatura olarak gönderilmiş.", $"ETTN: {invoice.Ettn}");
                return;
            }

            // Onay al
            var confirm = await DialogService.Confirm(
                $"{invoice.InvoiceNo} numaralı faturayı e-Fatura olarak göndermek istediğinizden emin misiniz?",
                "e-Fatura Gönder",
                new ConfirmOptions { OkButtonText = "Gönder", CancelButtonText = "İptal" });

            if (confirm != true) return;

            // Gönderim kilidini al
            _sendingInvoiceId = invoice.Id;
            _sendResultEttn = null;

            // Adımları hazırla (5 adım)
            _sendSteps = new List<EInvoiceSendStep>
            {
                new() { Name = "Token alınıyor...", Icon = "hourglass_empty" },
                new() { Name = "Kullanıcı tipi belirleniyor...", Icon = "hourglass_empty" },
                new() { Name = "Fatura bilgileri hazırlanıyor...", Icon = "hourglass_empty" },
                new() { Name = "Fatura Odaksoft'a gönderiliyor...", Icon = "hourglass_empty" },
                new() { Name = "Sonuç kontrol ediliyor...", Icon = "hourglass_empty" }
            };
            _showSendProgress = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                // ADIM 1: Token al
                await UpdateStep(0, EInvoiceStepStatus.InProgress);
                string token;
                try
                {
                    token = await OdaksoftAuthService.GetValidTokenAsync();
                    await UpdateStep(0, EInvoiceStepStatus.Success);
                }
                catch (Exception ex)
                {
                    await UpdateStep(0, EInvoiceStepStatus.Error, $"Token alınamadı: {ex.Message}");
                    return;
                }

                // ADIM 2: Kullanıcı tipi belirle (GİB CheckUser)
                await UpdateStep(1, EInvoiceStepStatus.InProgress);
                string profile;
                string musteriVkn = "", musteriAdres = "", musteriSehir = "";
                string musteriIlce = "", musteriVergiDairesi = "";
                string musteriEmail = "", musteriTelefon = "";
                InvoiceUpsertDto faturaDetay;
                try
                {
                    // Fatura detaylarını çek
                    var detailResponse = await InvoiceService.GetInvoiceById(invoice.Id);
                    if (!detailResponse.Ok || detailResponse.Result == null)
                    {
                        await UpdateStep(1, EInvoiceStepStatus.Error, "Fatura detayları alınamadı.");
                        return;
                    }

                    faturaDetay = detailResponse.Result;

                    // Müşteri bilgilerini çek
                    if (faturaDetay.CustomerId.HasValue && faturaDetay.CustomerId.Value > 0)
                    {
                        var customerResponse = await CustomerService.GetCustomerById(faturaDetay.CustomerId.Value);
                        if (customerResponse.Ok && customerResponse.Result != null)
                        {
                            var musteri = customerResponse.Result;
                            musteriVkn = musteri.TaxNumber ?? "";
                            musteriAdres = musteri.Address ?? "";
                            musteriSehir = musteri.CityName ?? "";
                            musteriIlce = musteri.TownName ?? musteri.District ?? "";
                            musteriVergiDairesi = musteri.TaxOffice ?? "";
                            musteriEmail = musteri.Email ?? "";
                            musteriTelefon = musteri.Phone ?? musteri.Mobile ?? "";
                        }
                    }

                    // GİB CheckUser ile kullanıcı tipini belirle
                    if (!string.IsNullOrWhiteSpace(musteriVkn))
                    {
                        var checkUserResult = await OdaksoftInvoiceService.CheckUserAsync(musteriVkn);
                        if (checkUserResult.Status)
                        {
                            profile = checkUserResult.Data ? "TEMELFATURA" : "EARSIVFATURA";
                            _sendSteps[1].Name = checkUserResult.Data 
                                ? "e-Fatura mükellefi (TEMELFATURA)" 
                                : "e-Arşiv fatura (EARSIVFATURA)";
                        }
                        else
                        {
                            profile = "EARSIVFATURA";
                            _sendSteps[1].Name = "Kullanıcı tipi belirlenemedi, e-Arşiv olarak devam ediliyor";
                        }
                    }
                    else
                    {
                        profile = "EARSIVFATURA";
                        _sendSteps[1].Name = "VKN bulunamadı, e-Arşiv olarak devam ediliyor";
                    }

                    await UpdateStep(1, EInvoiceStepStatus.Success);
                }
                catch (Exception ex)
                {
                    await UpdateStep(1, EInvoiceStepStatus.Error, $"Kullanıcı tipi belirlenemedi: {ex.Message}");
                    return;
                }

                // ADIM 3: Fatura bilgilerini hazırla
                await UpdateStep(2, EInvoiceStepStatus.InProgress);
                OdaksoftCreateInvoiceRequestDto request;
                try
                {
                    // Request DTO oluştur
                    request = new OdaksoftCreateInvoiceRequestDto
                    {
                        ItemDto = new List<OdaksoftInvoiceItemDto>
                        {
                            new OdaksoftInvoiceItemDto
                            {
                                Ettn = Guid.NewGuid().ToString(),
                                CurrencyCode = faturaDetay.CurrencyCode ?? "TRY",
                                InvoiceType = "SATIS",
                                Profile = profile,
                                DocNo = $"ODK{DateTime.Now.Year}{(faturaDetay.Id * 100 + DateTime.Now.Second).ToString().PadLeft(9, '0')}",
                                DocDate = DateTime.Now,
                                RefNo = Guid.NewGuid().ToString(),
                                IsCalculateByApi = true,
                                IsDraft = false,
                                Notes = !string.IsNullOrWhiteSpace(faturaDetay.Description)
                                    ? new List<string> { faturaDetay.Description }
                                    : null,
                                InvoiceAccount = new OdaksoftInvoiceAccountDto
                                {
                                    VknTckn = musteriVkn,
                                    AccountName = faturaDetay.CustomerName ?? "",
                                    TaxOfficeName = musteriVergiDairesi,
                                    CityName = musteriSehir,
                                    District = musteriIlce,
                                    CitySubdivision = musteriIlce,
                                    StreetName = musteriAdres,
                                    Email = musteriEmail,
                                    Telephone = musteriTelefon,
                                    CountryName = "Türkiye"
                                },
                                InvoiceDetail = faturaDetay.Items.Select(item =>
                                {
                                    // KDV dahil ise fiyattan KDV'yi çıkar, değilse direkt kullan
                                    var birimFiyat = faturaDetay.IsVatIncluded && item.VatRate > 0
                                        ? item.Price / (1 + item.VatRate / 100)
                                        : item.Price;
                                    birimFiyat = Math.Round(birimFiyat, 2);
                                    var tutar = Math.Round(item.Quantity * birimFiyat, 2);
                                    var kdvTutar = Math.Round(tutar * item.VatRate / 100, 2);

                                    return new OdaksoftInvoiceDetailDto
                                    {
                                        ProductCode = string.IsNullOrWhiteSpace(item.ProductCode) ? null : item.ProductCode,
                                        ProductName = item.ProductName,
                                        Quantity = item.Quantity,
                                        UnitCode = "C62",
                                        UnitPrice = birimFiyat,
                                        Amount = tutar,
                                        VatRate = item.VatRate,
                                        Tax = new List<OdaksoftInvoiceTaxDto>
                                        {
                                            new OdaksoftInvoiceTaxDto
                                            {
                                                TaxName = "KDV",
                                                TaxCode = "0015",
                                                TaxRate = item.VatRate,
                                                TaxAmount = kdvTutar
                                            }
                                        },
                                        AllowanceCharge = item.Discount1 > 0
                                            ? new List<OdaksoftAllowanceChargeDto>
                                            {
                                                new OdaksoftAllowanceChargeDto
                                                {
                                                    Rate = item.Discount1,
                                                    Amount = Math.Round(tutar * item.Discount1 / 100, 2),
                                                    Description = "İskonto"
                                                }
                                            }
                                            : null
                                    };
                                }).ToList()
                            }
                        }
                    };

                    await UpdateStep(2, EInvoiceStepStatus.Success);
                }
                catch (Exception ex)
                {
                    await UpdateStep(2, EInvoiceStepStatus.Error, $"Fatura bilgileri hazırlanamadı: {ex.Message}");
                    return;
                }

                // ADIM 4: Odaksoft'a gönder
                await UpdateStep(3, EInvoiceStepStatus.InProgress);
                CreateInvoiceResponseDto response;
                try
                {
                    response = await OdaksoftInvoiceService.CreateInvoiceAsync(request);
                    await UpdateStep(3, EInvoiceStepStatus.Success);
                }
                catch (Exception ex)
                {
                    await UpdateStep(3, EInvoiceStepStatus.Error, $"Gönderim hatası: {ex.Message}");
                    return;
                }

                // ADIM 5: Sonuç kontrol
                await UpdateStep(4, EInvoiceStepStatus.InProgress);
                if (response.Success)
                {
                    _sendResultEttn = response.Ettn;

                    // Veritabanında ETTN, durum ve fatura tipi bilgisini güncelle
                    var isEFatura = profile == "TEMELFATURA";
                    var updateResult = await InvoiceService.UpdateEInvoiceStatus(
                        invoice.Id, 
                        response.Ettn ?? "", 
                        "GONDERILDI",
                        isEFatura,
                        !isEFatura,
                        Security.User.Id);

                    if (!updateResult.Ok)
                    {
                        NotificationService.Notify(NotificationSeverity.Warning, 
                            "Fatura gönderildi ancak veritabanı güncellenemedi.", 
                            $"ETTN: {response.Ettn}");
                    }

                    _sendSteps[4].Name = "Fatura başarıyla gönderildi!";
                    await UpdateStep(4, EInvoiceStepStatus.Success);

                    // Grid'i yenile
                    if (radzenDataGrid != null)
                    {
                        await radzenDataGrid.Reload();
                    }
                }
                else
                {
                    await UpdateStep(4, EInvoiceStepStatus.Error, response.ErrorMessage ?? "Bilinmeyen hata");
                }
            }
            catch (Exception ex)
            {
                // Beklenmeyen genel hata - hangi adımdaysa onu hatalı işaretle
                var aktifAdim = _sendSteps.FindIndex(s => s.Status == EInvoiceStepStatus.InProgress);
                if (aktifAdim >= 0)
                {
                    await UpdateStep(aktifAdim, EInvoiceStepStatus.Error, $"Beklenmeyen hata: {ex.Message}");
                }
            }
            finally
            {
                // Gönderim kilidini serbest bırak
                _sendingInvoiceId = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// e-Fatura iptal butonuna tıklanınca çalışır.
        /// Odaksoft üzerinden faturayı iptal eder ve veritabanını günceller.
        /// </summary>
        protected async Task CancelEInvoiceClick(InvoiceListDto invoice)
        {
            // Mükerrer işlem koruması
            if (_cancellingInvoiceId != null) return;

            if (string.IsNullOrEmpty(invoice.Ettn))
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Bu fatura henüz gönderilmemiş, iptal edilemez.");
                return;
            }

            // Onay al
            var confirm = await DialogService.Confirm(
                $"{invoice.InvoiceNo} numaralı e-Faturayı iptal etmek istediğinizden emin misiniz?\nETTN: {invoice.Ettn}",
                "e-Fatura İptal",
                new ConfirmOptions { OkButtonText = "İptal Et", CancelButtonText = "Vazgeç" });

            if (confirm != true) return;

            _cancellingInvoiceId = invoice.Id;

            try
            {
                // Odaksoft'ta faturayı iptal et
                var (basarili, hataMesaji) = await OdaksoftInvoiceService.CancelInvoiceAsync(invoice.Ettn, "Kullanıcı tarafından iptal edildi");

                if (basarili)
                {
                    // Veritabanında durumu "IPTAL" olarak güncelle, ETTN'i koru
                    var updateResult = await InvoiceService.UpdateEInvoiceStatus(
                        invoice.Id,
                        invoice.Ettn!,
                        "IPTAL",
                        invoice.IsEInvoice,
                        invoice.IsEArchive,
                        Security.User.Id);

                    if (updateResult.Ok)
                    {
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Success,
                            Summary = "Başarılı",
                            Detail = $"{invoice.InvoiceNo} numaralı e-Fatura iptal edildi."
                        });
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Warning,
                            "Fatura Odaksoft'ta iptal edildi ancak veritabanı güncellenemedi.");
                    }

                    // Grid'i yenile
                    if (radzenDataGrid != null)
                    {
                        await radzenDataGrid.Reload();
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "e-Fatura iptal edilemedi", hataMesaji ?? "Bilinmeyen hata");
                }
            }
            catch (HttpRequestException httpEx)
            {
                NotificationService.Notify(NotificationSeverity.Error, "API Hatası", $"Odaksoft API hatası: {httpEx.Message}");
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İptal hatası", ex.Message);
            }
            finally
            {
                _cancellingInvoiceId = null;
                await InvokeAsync(StateHasChanged);
            }
        }

        /// <summary>
        /// e-Fatura durumuna göre badge stilini döndürür
        /// </summary>
        protected static BadgeStyle GetEInvoiceStatusBadgeStyle(string status)
        {
            return status?.ToUpperInvariant() switch
            {
                "GONDERILDI" => BadgeStyle.Info,
                "ONAYLANDI" => BadgeStyle.Success,
                "REDDEDILDI" => BadgeStyle.Danger,
                "IPTAL" => BadgeStyle.Danger,
                "TASLAK" => BadgeStyle.Warning,
                _ => BadgeStyle.Light
            };
        }

        // e-Fatura indirme state
        private int? _downloadingInvoiceId;

        /// <summary>
        /// e-Fatura PDF indirme butonuna tıklanınca çalışır.
        /// Odaksoft'tan faturayı indirir ve tarayıcıda açtırır.
        /// </summary>
        protected async Task DownloadEInvoiceClick(InvoiceListDto invoice)
        {
            // Mükerrer işlem koruması
            if (_downloadingInvoiceId != null) return;

            if (string.IsNullOrEmpty(invoice.Ettn))
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Bu fatura henüz gönderilmemiş, indirilemez.");
                return;
            }

            _downloadingInvoiceId = invoice.Id;

            try
            {
                var response = await OdaksoftInvoiceService.DownloadOutboxInvoiceAsync(invoice.Ettn);

                if (response.Status && !string.IsNullOrEmpty(response.ByteArray))
                {
                    // Base64 PDF'i tarayıcıda aç
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
                    // HTML olarak döndüyse yeni sekmede aç
                    await JSRuntime.InvokeVoidAsync("openHtmlInNewTab", new object?[] { response.Html });
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Fatura indirilemedi", response.Message ?? response.ExceptionMessage ?? "Bilinmeyen hata");
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, "İndirme hatası", ex.Message);
            }
            finally
            {
                _downloadingInvoiceId = null;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
