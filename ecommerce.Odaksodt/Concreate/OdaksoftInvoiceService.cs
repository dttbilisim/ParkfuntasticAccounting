using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Gib;
using ecommerce.Odaksodt.Dtos.Invoice;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace ecommerce.Odaksodt.Concreate;

/// <summary>
/// Odaksoft fatura servisi implementasyonu
/// </summary>
public class OdaksoftInvoiceService : IOdaksoftInvoiceService
{
    private readonly IOdaksoftHttpClient _httpClient;
    private readonly ILogger<OdaksoftInvoiceService> _logger;

    public OdaksoftInvoiceService(
        IOdaksoftHttpClient httpClient,
        ILogger<OdaksoftInvoiceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CreateInvoiceResponseDto> CreateInvoiceAsync(OdaksoftCreateInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var invoiceNo = request.ItemDto?.FirstOrDefault()?.DocNo ?? "Bilinmeyen";
            
            _logger.LogInformation("Fatura oluşturuluyor: {InvoiceNumber}", invoiceNo);

            var response = await _httpClient.PostWithAuthAsync<OdaksoftCreateInvoiceRequestDto, CreateInvoiceResponseDto>(
                "/api/IntegrationGidenFatura/Create",
                request,
                cancellationToken);

            if (response?.Success == true)
            {
                _logger.LogInformation("Fatura başarıyla oluşturuldu. ETTN: {Ettn}", response.Ettn);
            }
            else
            {
                _logger.LogWarning("Fatura oluşturma başarısız: {ErrorMessage}", response?.ErrorMessage);
            }

            return response ?? new CreateInvoiceResponseDto
            {
                Status = false,
                Message = "Beklenmeyen hata - API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Odaksoft API devre kesici aktif - ardışık hatalardan dolayı bağlantı geçici olarak kesildi");
            return new CreateInvoiceResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Ardışık hatalardan dolayı bağlantı geçici olarak kesildi. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura oluşturma sırasında hata");
            return new CreateInvoiceResponseDto
            {
                Status = false,
                Message = $"Fatura oluşturma hatası: {ex.Message}"
            };
        }
    }

    public async Task<InvoiceStatusResponseDto> GetInvoiceStatusAsync(InvoiceStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            if ((request.EttnList == null || request.EttnList.Count == 0) &&
                (request.RefNoList == null || request.RefNoList.Count == 0))
            {
                return new InvoiceStatusResponseDto
                {
                    Success = false,
                    ErrorMessage = "ETTN veya RefNo listesi boş olamaz"
                };
            }

            _logger.LogInformation("Fatura durumu sorgulanıyor");

            var response = await _httpClient.PostWithAuthAsync<InvoiceStatusRequestDto, InvoiceStatusResponseDto>(
                "/api/IntegrationGidenFatura/GetOutboxInvoiceStatus",
                request,
                cancellationToken);

            return response ?? new InvoiceStatusResponseDto
            {
                Success = false,
                ErrorMessage = "Beklenmeyen hata"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura durum sorgulama hatası");
            return new InvoiceStatusResponseDto
            {
                Success = false,
                ErrorMessage = $"Durum sorgulama hatası: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> DownloadInvoicePdfAsync(string ettn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fatura PDF indiriliyor: {Ettn}", ettn);

            var endpoint = $"/api/IntegrationGidenFatura/DownloadOutboxInvoice?ettn={ettn}&format=pdf";
            return await _httpClient.DownloadBinaryAsync(endpoint, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura PDF indirme hatası: {Ettn}", ettn);
            throw;
        }
    }

    public async Task<string> DownloadInvoiceHtmlAsync(string ettn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fatura HTML indiriliyor: {Ettn}", ettn);

            var endpoint = $"/api/IntegrationGidenFatura/PreviewInvoice?ettn={ettn}";
            var response = await _httpClient.PostWithAuthAsync<object, OdaksoftPreviewResponseDto>(endpoint, new { }, cancellationToken);

            return response?.Data ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura HTML indirme hatası: {Ettn}", ettn);
            throw;
        }
    }

    public async Task<bool> SendInvoiceToGibAsync(string ettn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fatura GİB'e gönderiliyor: {Ettn}", ettn);

            var request = new { Ettn = ettn };
            var response = await _httpClient.PostWithAuthAsync<object, OdaksoftSendInvoiceResponseDto>(
                "/api/IntegrationGidenFatura/SendInvoice",
                request,
                cancellationToken);

            var success = response?.Status == true;
            
            if (success)
            {
                _logger.LogInformation("Fatura GİB'e başarıyla gönderildi: {Ettn}", ettn);
            }
            else
            {
                _logger.LogWarning("Fatura GİB'e gönderilemedi: {Ettn}, Mesaj: {Mesaj}", ettn, response?.Message);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura GİB'e gönderme hatası: {Ettn}", ettn);
            return false;
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> CancelInvoiceAsync(string ettn, string cancelReason, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("E-Arşiv fatura iptal ediliyor: {Ettn}", ettn);

            // Odaksoft API ETTNList (array) formatında bekliyor
            var request = new
            {
                ETTNList = new[] { ettn },
                CancelReason = cancelReason
            };

            var response = await _httpClient.PostWithAuthAsync<object, OdaksoftCancelResponseDto>(
                "/api/IntegrationGidenFatura/CancelInvoice",
                request,
                cancellationToken);

            if (response?.Status == true)
            {
                _logger.LogInformation("Fatura başarıyla iptal edildi: {Ettn}", ettn);
                return (true, null);
            }
            else
            {
                var mesaj = response?.Message ?? "Bilinmeyen hata";
                _logger.LogWarning("Fatura iptal edilemedi: {Ettn}, Mesaj: {Mesaj}", ettn, mesaj);
                return (false, mesaj);
            }
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Fatura iptal - devre kesici aktif: {Ettn}", ettn);
            return (false, "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fatura iptal API hatası: {Ettn}", ettn);
            return (false, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura iptal sırasında beklenmeyen hata: {Ettn}", ettn);
            return (false, $"Beklenmeyen hata: {ex.Message}");
        }
    }

    public async Task<CheckUserResponseDto> CheckUserAsync(string vkn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("GİB kullanıcı tipi sorgulanıyor. VKN: {Vkn}", vkn);

            var request = new CheckUserRequestDto
            {
                DocumentType = "Invoice",
                Identifier = vkn
            };

            var response = await _httpClient.PostWithAuthAsync<CheckUserRequestDto, CheckUserResponseDto>(
                "/api/IntegrationGibKullaniciListe/CheckUser",
                request,
                cancellationToken);

            if (response != null)
            {
                _logger.LogInformation("GİB kullanıcı tipi sorgulandı. VKN: {Vkn}, e-Fatura mükellefi: {IsEInvoice}", vkn, response.Data);
                return response;
            }

            return new CheckUserResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "GİB kullanıcı sorgusu - devre kesici aktif");
            return new CheckUserResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GİB kullanıcı tipi sorgulama hatası. VKN: {Vkn}", vkn);
            return new CheckUserResponseDto
            {
                Status = false,
                Message = $"Kullanıcı tipi sorgulama hatası: {ex.Message}"
            };
        }
    }

    public async Task<DownloadOutboxInvoiceResponseDto> DownloadOutboxInvoiceAsync(string ettn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fatura indiriliyor. ETTN: {Ettn}", ettn);

            var request = new DownloadOutboxInvoiceRequestDto
            {
                EttnList = new List<string> { ettn },
                IsDefaultXslt = true
            };

            var response = await _httpClient.PostWithAuthAsync<DownloadOutboxInvoiceRequestDto, DownloadOutboxInvoiceResponseDto>(
                "/api/IntegrationGidenFatura/DownloadOutboxInvoice",
                request,
                cancellationToken);

            if (response?.Status == true)
            {
                _logger.LogInformation("Fatura başarıyla indirildi. ETTN: {Ettn}", ettn);
            }
            else
            {
                _logger.LogWarning("Fatura indirilemedi. ETTN: {Ettn}, Mesaj: {Mesaj}", ettn, response?.Message);
            }

            return response ?? new DownloadOutboxInvoiceResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Fatura indirme - devre kesici aktif: {Ettn}", ettn);
            return new DownloadOutboxInvoiceResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Fatura indirme API hatası: {Ettn}", ettn);
            return new DownloadOutboxInvoiceResponseDto
            {
                Status = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatura indirme sırasında beklenmeyen hata: {Ettn}", ettn);
            return new DownloadOutboxInvoiceResponseDto
            {
                Status = false,
                Message = $"Beklenmeyen hata: {ex.Message}"
            };
        }
    }

    public async Task<GetInboxInvoiceResponseDto> GetInboxInvoicesAsync(GetInboxInvoiceRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Gelen faturalar sorgulanıyor. Sayfa: {Page}, Boyut: {Size}", request.PageCount, request.PageSize);

            var response = await _httpClient.PostWithAuthAsync<GetInboxInvoiceRequestDto, GetInboxInvoiceResponseDto>(
                "/api/IntegrationGelenFatura/GetAllNewInboxInvoice",
                request,
                cancellationToken);

            if (response?.Status == true)
            {
                _logger.LogInformation("Gelen faturalar başarıyla alındı. Toplam: {Count}", response.TotalCount);
            }
            else
            {
                _logger.LogWarning("Gelen faturalar alınamadı: {Message}", response?.Message);
            }

            return response ?? new GetInboxInvoiceResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Gelen faturalar - devre kesici aktif");
            return new GetInboxInvoiceResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gelen faturalar sorgulama hatası");
            return new GetInboxInvoiceResponseDto
            {
                Status = false,
                Message = $"Gelen faturalar hatası: {ex.Message}"
            };
        }
    }

    public async Task<GetOutboxInvoiceFilterResponseDto> GetOutboxInvoicesAsync(GetOutboxInvoiceFilterRequestDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Giden faturalar sorgulanıyor. Sayfa: {Page}, Boyut: {Size}", request.PageCount, request.PageSize);

            var response = await _httpClient.PostWithAuthAsync<GetOutboxInvoiceFilterRequestDto, GetOutboxInvoiceFilterResponseDto>(
                "/api/IntegrationGidenFatura/GetOutboxInvoiceFilter",
                request,
                cancellationToken);

            if (response?.Status == true)
            {
                _logger.LogInformation("Giden faturalar başarıyla alındı. Toplam: {Count}", response.TotalCount);
            }
            else
            {
                _logger.LogWarning("Giden faturalar alınamadı: {Message}", response?.Message);
            }

            return response ?? new GetOutboxInvoiceFilterResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Giden faturalar - devre kesici aktif");
            return new GetOutboxInvoiceFilterResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Giden faturalar sorgulama hatası");
            return new GetOutboxInvoiceFilterResponseDto
            {
                Status = false,
                Message = $"Giden faturalar hatası: {ex.Message}"
            };
        }
    }

    public async Task<OdaksoftPreviewResponseDto> PreviewOutboxInvoiceAsync(string ettn, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Giden fatura önizleme. ETTN: {Ettn}", ettn);

            var request = new PreviewOutboxInvoiceRequestDto
            {
                Ettn = ettn,
                IsDefaultXslt = true
            };

            var response = await _httpClient.PostWithAuthAsync<PreviewOutboxInvoiceRequestDto, OdaksoftPreviewResponseDto>(
                "/api/IntegrationGidenFatura/PreviewInvoice",
                request,
                cancellationToken);

            return response ?? new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Giden fatura önizleme - devre kesici aktif: {Ettn}", ettn);
            return new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Giden fatura önizleme hatası: {Ettn}", ettn);
            return new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = $"Önizleme hatası: {ex.Message}"
            };
        }
    }

    public async Task<OdaksoftPreviewResponseDto> PreviewInboxInvoiceAsync(string ettn, string? refNo = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Gelen fatura önizleme. ETTN: {Ettn}", ettn);

            var request = new PreviewInboxInvoiceRequestDto
            {
                Ettn = ettn,
                RefNo = refNo
            };

            var response = await _httpClient.PostWithAuthAsync<PreviewInboxInvoiceRequestDto, OdaksoftPreviewResponseDto>(
                "/api/IntegrationGelenFatura/PreviewInvoice",
                request,
                cancellationToken);

            return response ?? new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = "API'den yanıt alınamadı"
            };
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Gelen fatura önizleme - devre kesici aktif: {Ettn}", ettn);
            return new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = "Odaksoft API'ye bağlanılamıyor. Lütfen 15 saniye bekleyip tekrar deneyin."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gelen fatura önizleme hatası: {Ettn}", ettn);
            return new OdaksoftPreviewResponseDto
            {
                Status = false,
                Message = $"Önizleme hatası: {ex.Message}"
            };
        }
    }
}
