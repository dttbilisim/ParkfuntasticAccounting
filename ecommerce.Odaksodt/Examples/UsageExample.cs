using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Dtos.Invoice;

namespace ecommerce.Odaksodt.Examples;

/// <summary>
/// Odaksoft E-Fatura entegrasyonu kullanım örnekleri
/// </summary>
public class UsageExample
{
    private readonly IOdaksoftInvoiceService _invoiceService;

    public UsageExample(IOdaksoftInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    /// <summary>
    /// Örnek 1: Basit satış faturası oluşturma
    /// </summary>
    public async Task<string> CreateSimpleSalesInvoiceAsync()
    {
        var request = new OdaksoftCreateInvoiceRequestDto
        {
            ItemDto = new List<OdaksoftInvoiceItemDto>
            {
                new OdaksoftInvoiceItemDto
                {
                    CurrencyCode = "TRY",
                    InvoiceType = "SATIS",
                    Profile = "TEMELFATURA",
                    DocNo = "FTR2024000001",
                    DocDate = DateTime.Now,
                    RefNo = "ORDER-12345",
                    IsCalculateByApi = true,
                    IsDraft = false,
                    InvoiceAccount = new OdaksoftInvoiceAccountDto
                    {
                        VknTckn = "1234567890",
                        AccountName = "Örnek Müşteri A.Ş.",
                        TaxOfficeName = "Kadıköy",
                        StreetName = "Örnek Mahallesi, Örnek Sokak No:1",
                        CityName = "İstanbul",
                        District = "Kadıköy",
                        CitySubdivision = "Kadıköy", // API tarafından zorunlu
                        CountryName = "Türkiye",
                        Email = "info@ornek.com",
                        Telephone = "02121234567"
                    },
                    InvoiceDetail = new List<OdaksoftInvoiceDetailDto>
                    {
                        new OdaksoftInvoiceDetailDto
                        {
                            ProductName = "Fren Balata Takımı",
                            Quantity = 2,
                            UnitCode = "C62",
                            UnitPrice = 500.00m,
                            Amount = 1000.00m,
                            ProductCode = "FRN-001",
                            VatRate = 20, // API tarafından required alan
                            Tax = new List<OdaksoftInvoiceTaxDto>
                            {
                                new OdaksoftInvoiceTaxDto
                                {
                                    TaxName = "KDV",
                                    TaxCode = "0015",
                                    TaxRate = 20,
                                    TaxAmount = 200.00m
                                }
                            },
                            AllowanceCharge = new List<OdaksoftAllowanceChargeDto>
                            {
                                new OdaksoftAllowanceChargeDto
                                {
                                    Rate = 10,
                                    Amount = 100.00m,
                                    Description = "İskonto"
                                }
                            }
                        },
                        new OdaksoftInvoiceDetailDto
                        {
                            ProductName = "Motor Yağı 5W-30",
                            Quantity = 4,
                            UnitCode = "LTR",
                            UnitPrice = 150.00m,
                            Amount = 600.00m,
                            ProductCode = "YAG-002",
                            VatRate = 20, // API tarafından required alan
                            Tax = new List<OdaksoftInvoiceTaxDto>
                            {
                                new OdaksoftInvoiceTaxDto
                                {
                                    TaxName = "KDV",
                                    TaxCode = "0015",
                                    TaxRate = 20,
                                    TaxAmount = 120.00m
                                }
                            }
                        }
                    },
                    Notes = new List<string> { "Sipariş numarası: ORDER-12345" }
                }
            }
        };

        var response = await _invoiceService.CreateInvoiceAsync(request);

        if (response.Success)
        {
            Console.WriteLine($"Fatura başarıyla oluşturuldu. ETTN: {response.Ettn}");
            return response.Ettn!;
        }

        throw new Exception($"Fatura oluşturulamadı: {response.ErrorMessage}");
    }

    /// <summary>
    /// Örnek 2: E-Arşiv fatura oluşturma (bireysel müşteri)
    /// </summary>
    public async Task<string> CreateEArchiveInvoiceAsync()
    {
        var request = new OdaksoftCreateInvoiceRequestDto
        {
            ItemDto = new List<OdaksoftInvoiceItemDto>
            {
                new OdaksoftInvoiceItemDto
                {
                    CurrencyCode = "TRY",
                    InvoiceType = "SATIS",
                    Profile = "EARSIVFATURA",
                    DocNo = "EAR2024000001",
                    DocDate = DateTime.Now,
                    IsCalculateByApi = true,
                    IsDraft = false,
                    InvoiceAccount = new OdaksoftInvoiceAccountDto
                    {
                        VknTckn = "12345678901", // TC Kimlik No (11 haneli)
                        AccountName = "Ahmet Yılmaz",
                        StreetName = "Örnek Mahallesi, Örnek Sokak No:5 Daire:3",
                        CityName = "Ankara",
                        District = "Çankaya",
                        CitySubdivision = "Çankaya", // API tarafından zorunlu
                        CountryName = "Türkiye",
                        Email = "ahmet@example.com",
                        Telephone = "05321234567"
                    },
                    InvoiceDetail = new List<OdaksoftInvoiceDetailDto>
                    {
                        new OdaksoftInvoiceDetailDto
                        {
                            ProductName = "Lastik Seti (4 Adet)",
                            Quantity = 1,
                            UnitCode = "C62",
                            UnitPrice = 2000.00m,
                            Amount = 2000.00m,
                            VatRate = 20, // API tarafından required alan
                            Tax = new List<OdaksoftInvoiceTaxDto>
                            {
                                new OdaksoftInvoiceTaxDto
                                {
                                    TaxName = "KDV",
                                    TaxCode = "0015",
                                    TaxRate = 20,
                                    TaxAmount = 400.00m
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = await _invoiceService.CreateInvoiceAsync(request);
        return response.Ettn!;
    }

    /// <summary>
    /// Örnek 3: Fatura durumu sorgulama
    /// </summary>
    public async Task CheckInvoiceStatusAsync(string ettn)
    {
        var request = new InvoiceStatusRequestDto
        {
            EttnList = new List<string> { ettn }
        };

        var response = await _invoiceService.GetInvoiceStatusAsync(request);

        if (response.Success && response.Invoices.Any())
        {
            var invoice = response.Invoices.First();
            Console.WriteLine($"Fatura No: {invoice.InvoiceNumber}");
            Console.WriteLine($"Durum: {invoice.Status}");
            Console.WriteLine($"GİB Durumu: {invoice.GibStatus}");
            Console.WriteLine($"Açıklama: {invoice.StatusDescription}");
        }
    }

    /// <summary>
    /// Örnek 4: Fatura PDF indirme ve kaydetme
    /// </summary>
    public async Task DownloadAndSaveInvoicePdfAsync(string ettn, string savePath)
    {
        var pdfBytes = await _invoiceService.DownloadInvoicePdfAsync(ettn);
        await File.WriteAllBytesAsync(savePath, pdfBytes);
        Console.WriteLine($"Fatura PDF kaydedildi: {savePath}");
    }

    /// <summary>
    /// Örnek 5: Taslak faturayı GİB'e gönderme
    /// </summary>
    public async Task SendDraftInvoiceToGibAsync(string ettn)
    {
        var success = await _invoiceService.SendInvoiceToGibAsync(ettn);

        if (success)
        {
            Console.WriteLine($"Fatura GİB'e başarıyla gönderildi: {ettn}");
        }
        else
        {
            Console.WriteLine($"Fatura GİB'e gönderilemedi: {ettn}");
        }
    }

    /// <summary>
    /// Örnek 6: E-Arşiv fatura iptali
    /// </summary>
    public async Task CancelEArchiveInvoiceAsync(string ettn)
    {
        var (basarili, hataMesaji) = await _invoiceService.CancelInvoiceAsync(ettn, "Müşteri talebi üzerine iptal");

        if (basarili)
        {
            Console.WriteLine($"Fatura başarıyla iptal edildi: {ettn}");
        }
        else
        {
            Console.WriteLine($"Fatura iptal edilemedi: {ettn} - Hata: {hataMesaji}");
        }
    }

    /// <summary>
    /// Örnek 7: Toplu fatura durum sorgulama
    /// </summary>
    public async Task CheckMultipleInvoicesAsync(List<string> ettnList)
    {
        var request = new InvoiceStatusRequestDto
        {
            EttnList = ettnList
        };

        var response = await _invoiceService.GetInvoiceStatusAsync(request);

        if (response.Success)
        {
            Console.WriteLine($"Toplam {response.Invoices.Count} fatura sorgulandı:");
            
            foreach (var invoice in response.Invoices)
            {
                Console.WriteLine($"- {invoice.InvoiceNumber}: {invoice.Status}");
            }
        }
    }

    /// <summary>
    /// Örnek 8: Referans numarası ile fatura sorgulama
    /// </summary>
    public async Task CheckInvoiceByReferenceNumberAsync(string refNo)
    {
        var request = new InvoiceStatusRequestDto
        {
            RefNoList = new List<string> { refNo }
        };

        var response = await _invoiceService.GetInvoiceStatusAsync(request);

        if (response.Success && response.Invoices.Any())
        {
            var invoice = response.Invoices.First();
            Console.WriteLine($"Referans No: {refNo}");
            Console.WriteLine($"ETTN: {invoice.Ettn}");
            Console.WriteLine($"Durum: {invoice.Status}");
        }
    }
}
