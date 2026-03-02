using ecommerce.Admin.Domain.Dtos.ReportDto;
using ecommerce.Admin.Domain.Report;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.JSInterop;

namespace ecommerce.Admin.Components.Pages.Report;
public partial class SellerCountOrderPage
{
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected IReportService _reportService { get; set; }

    protected RadzenDataGrid<SellerCountOrderDto>? radzenDataGrid = new();
    protected List<SellerCountOrderDto> data = new();
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
        var rsp = await _reportService.Execute<SellerCountOrderDto>("fn_report_seller_count_orders", parameter);
        count = rsp.Count;
        data = rsp;
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

            workSheet.Cells[1, 1].Value = "Alıcı";
            workSheet.Cells[1, 2].Value = "Sipariş Adet";
            workSheet.Cells[1, 3].Value = "Toplam";
            workSheet.Cells[1, 4].Value = "Alıcı Komisyon";
            workSheet.Cells[1, 5].Value = "ecommerce Komisyon";
            workSheet.Cells[1, 6].Value = "Iyzico Komisyon";
            workSheet.Cells[1, 7].Value = "İndirimler";
            workSheet.Cells[1, 8].Value = "Kargo";
            var recordIndex = 2;

            foreach (var item in data.ToList())
            {
                workSheet.Cells[recordIndex, 1].Value = item.AccountName;
                workSheet.Cells[recordIndex, 2].Value = item.Count;
                workSheet.Cells[recordIndex, 3].Value = item.Total;
                workSheet.Cells[recordIndex, 4].Value = item.SubMerhant;
                workSheet.Cells[recordIndex, 5].Value = item.Merhant;
                workSheet.Cells[recordIndex, 6].Value = item.Iyzico;
                workSheet.Cells[recordIndex, 7].Value = item.DiscountTotal;
                workSheet.Cells[recordIndex, 8].Value = item.CargoTotal;

                recordIndex++;
            }
            workSheet.Column(1).AutoFit();
            workSheet.Column(2).AutoFit();
            workSheet.Column(3).AutoFit();
            workSheet.Column(4).AutoFit();
            workSheet.Column(5).AutoFit();  
            workSheet.Column(6).AutoFit();  
            workSheet.Column(7).AutoFit();      
            workSheet.Column(8).AutoFit();  
            var fileBytes = await excel.GetAsByteArrayAsync();
            using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
            await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"ecommerce-ToplamSiparisRaporu.xlsx", streamRef);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

    }
}
