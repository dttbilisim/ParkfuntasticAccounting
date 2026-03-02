using ecommerce.Admin.Domain.Dtos.ReportDto;
using ecommerce.Admin.Domain.Report;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OfficeOpenXml.Style;
using OfficeOpenXml;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Report;
public partial class TotalReportPage{

   private bool allowVirtualization = false;
   int count;
   private IEnumerable<TotalReportDto> report;
   protected DateTime param1{get;set;}
   protected DateTime param2{get;set;}
    [Inject] protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected IReportService _reportService{get;set;}
   
   
   protected RadzenDataGrid<TotalReportDto> ? radzenDataGrid = new();
   protected override async Task OnInitializedAsync(){
      param1 = DateTime.Now.AddDays(-100);
      param2 = DateTime.Now;
    
   }
   private async Task CallReport(){
      var parameter = new{date1 = param1.Date, date2 = param2.Date};
      var rsp = await _reportService.Execute<TotalReportDto>("fn_report_total", parameter);
      count = rsp.Count;
      report = rsp;
      StateHasChanged();
        if (rsp.Count > 0)
            showExcelButton = true;
    }
    public async Task ExcelExportClick()
    {
        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
            workSheet.TabColor = System.Drawing.Color.Black;
            workSheet.DefaultRowHeight = 12;
            workSheet.Row(1).Height = 20;
            workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Row(1).Style.Font.Bold = true;
            workSheet.Column(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Column(2).Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            workSheet.Cells[1, 1].Value = "GENEL TOPLAM RAPORU";
            workSheet.Cells[2, 1].Value = "Net Satış Tutarı (Tamamlanan Sipariş)";
            workSheet.Cells[3, 1].Value = "Sipariş Adeti";
            workSheet.Cells[4, 1].Value = "İade Tutarı";
            workSheet.Cells[5, 1].Value = "İade Adeti";
            workSheet.Cells[6, 1].Value = "Kampanyasız Sipariş Toplamı";
            workSheet.Cells[7, 1].Value = "Kampanyalı Sipariş Toplamı";
            workSheet.Cells[8, 1].Value = "Sepette Bekleyen Sipariş Sayısı";
            
            workSheet.Cells[9, 1].Value = "Sepette Bekleyen Sipariş Tutarı";
            
            workSheet.Cells[10, 1].Value = "Aktif Alıcı Eczane Sayısı (İlanı Olan)";
            workSheet.Cells[11, 1].Value = "Aktif Satıcı Eczane Sayısı (İlanı Olan)";
            workSheet.Cells[12, 1].Value = "Aktif Alıcı ve Satıcı Eczane Sayısı (İlanı Olan)";
            workSheet.Cells[13, 1].Value = "Yeni Aktif Alıcı Eczane Sayısı (İlanı Olmayan)";
            workSheet.Cells[14, 1].Value = "Yeni Aktif Satıcı Eczane Sayısı (İlanı Olmayan)";
            workSheet.Cells[15, 1].Value = "Yeni Aktif Alıcı ve Satıcı Eczane Sayısı (İlanı Olmayan)";
            workSheet.Cells[16, 1].Value = "Kayıt Olmuş Giriş Yapmış Üye Sayısı";
            workSheet.Cells[17, 1].Value = "Kayıt Olmuş Giriş Yapmamış Üye Sayısı";
            
            workSheet.Cells[18, 1].Value = "Kayıt Olmuş Sipariş Vermemiş Eczane Sayısı";
            workSheet.Cells[19, 1].Value = "Kayıt Olmuş Sipariş Vermiş Eczane Sayısı";
            
            workSheet.Cells[20, 1].Value = "Aranan Ürün Sayısı";
            workSheet.Cells[21, 1].Value = "Toplam Kullanıcı Sayısı";
            workSheet.Cells[22, 1].Value = "Aktif Eczane Sayısı";
            workSheet.Cells[23, 1].Value = "Avm Eczane Sayısı";
            workSheet.Cells[24, 1].Value = "Cadde Eczane Sayısı";
            workSheet.Cells[25, 1].Value = "Semt Eczane Sayısı";
            workSheet.Cells[26, 1].Value = "Asm Eczane Sayısı";
            workSheet.Cells[27, 1].Value = "Hastane Yakini Eczane Sayısı";
            workSheet.Cells[28, 1].Value = "İlanı Olan Depo Sayısı";
            workSheet.Cells[29, 1].Value = "İlanı Olan Eczane Sayısı";
            workSheet.Cells[30, 1].Value = "İlanı Olan İlaç Firması Sayısı";
            workSheet.Cells[31, 1].Value = "Reçeteli İlan Sayısı";
            workSheet.Cells[32, 1].Value = "Anne Bebek İlan Sayısı";
            workSheet.Cells[33, 1].Value = "Besin Takviyesi İlan Sayısı";
            workSheet.Cells[34, 1].Value = "Medikal İlan Sayısı";
            workSheet.Cells[35, 1].Value = "Kişisel Bakım İlan Sayısı";
            workSheet.Cells[36, 1].Value = "Sağlık Ürünleri İlan Sayısı";
            workSheet.Cells[37, 1].Value = "Sarf Malzeme İlan Sayısı";
            workSheet.Cells[38, 1].Value = "Kategorisiz İlan Sayısı";
            workSheet.Cells[39, 1].Value = "Reçeteli İlan Total";
            workSheet.Cells[40, 1].Value = "Anne Bebek İlan Total";
            workSheet.Cells[41, 1].Value = "Besin Takviyesi İlan Total";
            workSheet.Cells[42, 1].Value = "Medikal İlan Total";
            workSheet.Cells[43, 1].Value = "Kişisel Bakım İlan Total";
            workSheet.Cells[44, 1].Value = "Sağlık Ürünleri İlan Total";
            workSheet.Cells[45, 1].Value = "Kategorisiz İlan Total";
            workSheet.Cells[46, 1].Value = "Puan Sayısı";
            workSheet.Cells[47, 1].Value = "Yorum Sayısı";

            var recordIndex = 2;
            foreach (var item in report.ToList())
            {
                workSheet.Cells[2, 2].Value = item.NetSatisTutari.ToString("n");
                workSheet.Cells[3, 2].Value = item.SiparisAdeti;
                workSheet.Cells[4, 2].Value = item.IadeTutari.ToString("n");
                workSheet.Cells[5, 2].Value = item.IadeAdeti;
                workSheet.Cells[6, 2].Value = item.KampanyasizSiparis.ToString("n");
                workSheet.Cells[7, 2].Value = item.KampanyanliSiparis.ToString("n");
                workSheet.Cells[8, 2].Value = item.SepetteBekleyenUrunlerAdet;
                workSheet.Cells[9, 2].Value = item.SepetteBekleyenUrunlerToplami.ToString("n");
                
                workSheet.Cells[10, 2].Value = item.buyercount;
                workSheet.Cells[11, 2].Value = item.sellercount;
                workSheet.Cells[12, 2].Value = item.buyerandsellercount;
                workSheet.Cells[13, 2].Value = item.newbuyercount;
                workSheet.Cells[14, 2].Value = item.newsellercount;
                workSheet.Cells[15, 2].Value = item.newbuyerandsellercount;
                
                
                workSheet.Cells[16, 2].Value = item.KayitOlmusGirisYapmis;
                workSheet.Cells[17, 2].Value = item.KayitOlmusGirisYapmamis;
                workSheet.Cells[18, 2].Value = item.LoginOlmusSiparisVermemis;
                workSheet.Cells[19, 2].Value = item.LoginOlmusSiparisVermis;
                workSheet.Cells[20, 2].Value = item.ArananUrunSayisi;
                workSheet.Cells[21, 2].Value = item.ToplamKullaniciSayisi;
                workSheet.Cells[22, 2].Value = item.AktifEczaneSayisi;
                workSheet.Cells[23, 2].Value = item.AvmEczanesi;
                workSheet.Cells[24, 2].Value = item.CaddeEczanesi;
                workSheet.Cells[25, 2].Value = item.SemtEczanesi;
                workSheet.Cells[26, 2].Value = item.AsmEczanesi;
                workSheet.Cells[27, 2].Value = item.HastaneYakiniEczanesi;
                workSheet.Cells[28, 2].Value = item.IlaniOlanDepo;
                workSheet.Cells[29, 2].Value = item.IlaniOlanEczane;
                workSheet.Cells[30, 2].Value = item.IlaniOlanIlacFirmasi;
                workSheet.Cells[31, 2].Value = item.ReceteliIlan;
                workSheet.Cells[32, 2].Value = item.AnneBebekIlan;
                workSheet.Cells[33, 2].Value = item.BesinTakviyesiIlan;
                workSheet.Cells[34, 2].Value = item.MedikaliIlan;
                workSheet.Cells[35, 2].Value = item.KisiselBakimiIlan;
                workSheet.Cells[36, 2].Value = item.SaglikUrunleriIlan;
                workSheet.Cells[37, 2].Value = item.SarfMalzemeIlan;
                workSheet.Cells[38, 2].Value = item.KategorisizIlan;
                workSheet.Cells[39, 2].Value = item.ReceteliTotal.ToString("n");
                workSheet.Cells[40, 2].Value = item.AnneBebekTotal.ToString("n");   
                workSheet.Cells[41, 2].Value = item.BesinTakviyesiTotal.ToString("n");  
                workSheet.Cells[42, 2].Value = item.MedikalTotal.ToString("n");     
                workSheet.Cells[43, 2].Value = item.KisiselTotal.ToString("n");     
                workSheet.Cells[44, 2].Value = item.SaglikUrunleriTotal.ToString("n");
                workSheet.Cells[45, 2].Value = item.KategorisizTotal.ToString("n");         
                workSheet.Cells[46, 2].Value = item.PuanSayisi;
                workSheet.Cells[47, 2].Value = item.YorumSayisi;


                recordIndex++;
            }
            workSheet.Column(1).AutoFit();
            workSheet.Column(2).AutoFit();
           
            var fileBytes = await excel.GetAsByteArrayAsync();
            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
            await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"ecommerce-GenelRapor.xlsx", streamRef);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    }

