using ecommerce.Admin.Domain.Dtos.ReportDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Report;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using OfficeOpenXml;
using OfficeOpenXml.Style;
namespace ecommerce.Admin.Components.Pages.Report;
public partial class BestSellerProductPage
{
    [Inject]  protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected IReportService _reportService { get; set; }
    [Inject] protected ICompanyService _companyService { get; set; }
    protected RadzenDataGrid<BestSellerproductDto>? radzenDataGrid = new();
    protected List<BestSellerproductDto> data = new();
    int count;
    protected DateTime param1 { get; set; }
    protected DateTime param2 { get; set; }
    protected override Task OnInitializedAsync()
    {
        param1 = DateTime.Now.AddDays(-100);
        param2 = DateTime.Now;
        return Task.CompletedTask;
    }
    private async Task LoadData(LoadDataArgs args)
    {
        try
        {

            await CallReport();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            throw;
        }
    }
    private async Task CallReport()
    {
        var parameter = new { date1 = param1.Date, date2 = param2.Date };
        var rsp = await _reportService.Execute<BestSellerproductDto>("fn_report_bestSellerProduct", parameter);
        count = rsp.Count;
        data = rsp;
        StateHasChanged();
        if(rsp.Count > 0)  
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
            workSheet.Cells[1, 2].Value = "Marka";
            workSheet.Cells[1, 3].Value = "Miktar";
            workSheet.Cells[1, 4].Value = "Toplam";
            var recordIndex = 2;
            foreach (var item in data.ToList())
            {
                workSheet.Cells[recordIndex, 1].Value = item.ProductName;
                workSheet.Cells[recordIndex, 2].Value = item.Brand;
                workSheet.Cells[recordIndex, 3].Value = item.Quantity;
                workSheet.Cells[recordIndex, 4].Value = item.Total;

                recordIndex++;
            }
            workSheet.Column(1).AutoFit();
            workSheet.Column(2).AutoFit();
            workSheet.Column(3).AutoFit();
            workSheet.Column(4).AutoFit();
            var fileBytes = await excel.GetAsByteArrayAsync();
            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
            await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"ecommerce-EnCokSatilanUrunler.xlsx", streamRef);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }
}
   

