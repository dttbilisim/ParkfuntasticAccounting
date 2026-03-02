using Microsoft.Extensions.Configuration;
using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ecommerce.Admin.Services.Concreate;

public class PaymentReceiptPdfService : IPaymentReceiptPdfService
{
    private readonly IConfiguration _configuration;

    public PaymentReceiptPdfService(IConfiguration configuration)
    {
        _configuration = configuration;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(PaymentReceiptDto receipt)
    {
        var companyName = _configuration["Company:CompanyName"] ?? _configuration["AppSettings:CompanyName"] ?? "Bicops";
        var companyVat = _configuration["Company:CompanyVat"] ?? _configuration["AppSettings:CompanyVat"] ?? "";
        var companyVatName = _configuration["Company:CompanyVatName"] ?? _configuration["AppSettings:CompanyVatName"] ?? "";
        var companyAddress = _configuration["Company:CompanyAddress"] ?? _configuration["AppSettings:CompanyAddress"] ?? "";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.B5.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().AlignCenter().Text(companyName).Bold().FontSize(12).FontColor(Colors.Grey.Darken2);
                        column.Item().AlignCenter().Text("TAHSİLAT MAKBUZU")
                            .Bold().FontSize(16).FontColor(Colors.Blue.Darken2);
                        if (!string.IsNullOrWhiteSpace(receipt.MakbuzNo))
                            column.Item().AlignCenter().PaddingTop(2).Text($"Makbuz No: {receipt.MakbuzNo}")
                                .Bold().FontSize(11).FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                page.Content()
                    .PaddingVertical(8)
                    .Column(column =>
                    {
                        column.Spacing(6);

                        if (!string.IsNullOrWhiteSpace(receipt.MakbuzNo))
                        {
                            column.Item()
                                .Padding(10)
                                .Background(Colors.Blue.Lighten4)
                                .Border(1).BorderColor(Colors.Blue.Lighten2)
                                .Row(row =>
                                {
                                    row.RelativeItem().Text("Makbuz No:").Bold().FontSize(11).FontColor(Colors.Blue.Darken2);
                                    row.RelativeItem(2).AlignRight().Text(receipt.MakbuzNo).Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                                });
                            column.Item().PaddingBottom(4);
                        }

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Cari Unvan:").Bold();
                            row.RelativeItem(2).Text(receipt.CustomerName);
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Cari Kodu:").Bold();
                            row.RelativeItem(2).Text(receipt.CustomerCode);
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("İşlem Tarihi:").Bold();
                            row.RelativeItem(2).Text(receipt.TransactionDate.ToString("dd.MM.yyyy HH:mm"));
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Ödeme Türü:").Bold();
                            row.RelativeItem(2).Text(receipt.PaymentTypeName ?? "Nakit/Kredi Kartı");
                        });
                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Açıklama:").Bold();
                            row.RelativeItem(2).Text(receipt.Description ?? "Tahsilat");
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Tahsil Edilen Tutar:").Bold().FontSize(12);
                            row.RelativeItem().AlignRight().Text($"₺{receipt.Amount:N2}").Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                        });

                        column.Item().PaddingTop(12).AlignCenter()
                            .Text($"Bu makbuz {receipt.TransactionDate:dd.MM.yyyy} tarihinde oluşturulmuştur.")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                    });

                page.Footer()
                    .Column(footer =>
                    {
                        if (!string.IsNullOrWhiteSpace(companyAddress))
                            footer.Item().AlignCenter().Text(companyAddress).FontSize(8).FontColor(Colors.Grey.Medium);
                        var vatParts = new[] { companyVatName, companyVat }.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        if (vatParts.Count > 0)
                            footer.Item().AlignCenter().Text(string.Join(" ", vatParts)).FontSize(8).FontColor(Colors.Grey.Medium);
                        footer.Item().AlignCenter().Text($"{companyName} Tahsilat Makbuzu").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
            });
        }).GeneratePdf();
    }
}
