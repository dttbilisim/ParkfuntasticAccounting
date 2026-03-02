using ecommerce.Admin.Domain.Dtos.ReportDto;
using ecommerce.Admin.Domain.Report;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.JSInterop;
namespace ecommerce.Admin.Components.Pages.Report;
public partial class ProductAdvertPage{
    [Inject] protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected IReportService _reportService{get;set;}
    protected RadzenDataGrid<ProductAdvertDto> ? radzenDataGrid = new();
    protected List<ProductAdvertDto> data = new();
    protected List<ProductAdvertDto> dataexcel = new();
    private int count;
    private int skip = 0;
    private int take = 20;
    private async Task LoadData(LoadDataArgs args){
        try{
            skip = Convert.ToInt32(args.Skip);
            take = Convert.ToInt32(args.Top);
            await CallReport();
            StateHasChanged();
        } catch(Exception e){
            Console.WriteLine(e.Message);
            throw;
        }
    }
    private async Task CallReport(){
        var parameter = new{};
        var rsp = await _reportService.Execute<ProductAdvertDto>("fn_report_product_advert", parameter);
        count = rsp.Count;
        data = rsp.Skip(skip).Take(take).ToList();
        dataexcel = rsp.ToList();    


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

            workSheet.Cells[1, 1].Value = "Ürün Adı";
            workSheet.Cells[1, 2].Value = "Barkod";
            workSheet.Cells[1, 3].Value = "İlan Sayısı";
            workSheet.Cells[1, 4].Value = "Ortalama Fiyat";
            workSheet.Cells[1, 5].Value = "Yüksek Fiyat";
            workSheet.Cells[1, 6].Value = "Düşük Fiyat";
            workSheet.Cells[1, 7].Value = "PSF";

            var recordIndex = 2;
            

            foreach (var item in dataexcel.ToList())
            {
                workSheet.Cells[recordIndex, 1].Value = item.ProductName;
                workSheet.Cells[recordIndex, 2].Value = item.Barcode;
                workSheet.Cells[recordIndex, 3].Value = item.AdvertCount;
                workSheet.Cells[recordIndex, 4].Value = item.AvgPrice;
                workSheet.Cells[recordIndex, 5].Value = item.MaxPrice;
                workSheet.Cells[recordIndex, 6].Value = item.MinPrice;
                workSheet.Cells[recordIndex, 7].Value = item.RetailPrice;

                recordIndex++;
            }
            workSheet.Column(1).AutoFit();
            workSheet.Column(2).AutoFit();
            workSheet.Column(3).AutoFit();
            workSheet.Column(4).AutoFit();
            workSheet.Column(5).AutoFit();
            workSheet.Column(6).AutoFit();
           workSheet.Column(7).AutoFit();
            
            var fileBytes = await excel.GetAsByteArrayAsync();
            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
            await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"ecommerce-UrunBazliIlanDurumu.xlsx", streamRef);
        }
        catch (Exception e)
        {

            Console.WriteLine(e);

        }
    }


}
