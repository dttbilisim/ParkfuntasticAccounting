using ecommerce.Admin.Domain.Dtos.ReportDto;
using ecommerce.Admin.Domain.Report;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Report;
public partial class OfferPage{
    public bool showExcelButton;
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected IReportService _reportService{get;set;}
   
    protected RadzenDataGrid<OfferReportDto> ? radzenDataGrid = new();
    protected List<OfferReportDto> data = new();
    int count;
    private async Task LoadData(LoadDataArgs args){
        try{
            await CallReport();
        } catch(Exception e){
            Console.WriteLine(e.Message);
            throw;
        }
    }
    private async Task CallReport(){
        var parameter = new{};
        var rsp = await _reportService.Execute<OfferReportDto>("fn_report_web_lansman", parameter);
        count = rsp.Count;
        data = rsp;
        StateHasChanged();
        if(rsp.Count > 0) 
            showExcelButton = true;
    }
    public async Task ExcelExportClick(){
        try{
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var excel = new ExcelPackage();
            var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
            workSheet.TabColor = System.Drawing.Color.Black;
            workSheet.DefaultRowHeight = 12;
            workSheet.Row(1).Height = 20;
            workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            workSheet.Row(1).Style.Font.Bold = true;
            workSheet.Cells[1, 1].Value = "Kullanıcı";
            workSheet.Cells[1, 2].Value = "Kullanıcı Tipi";
            workSheet.Cells[1, 3].Value = "Çalışma Tipi";
            workSheet.Cells[1, 4].Value = "İlk Alışveriş";
            workSheet.Cells[1, 5].Value = "10.000 tl Alışveriş";
            workSheet.Cells[1, 6].Value = "5 Satış Yapan Depo veya Marka";
    
            var recordIndex = 2;
            foreach(var item in data.ToList()){
                workSheet.Cells[recordIndex, 1].Value = item.Company;
                workSheet.Cells[recordIndex, 2].Value = item.UserType;
                workSheet.Cells[recordIndex, 3].Value = item.CustomerWorkingType;


                
                workSheet.Cells[recordIndex, 4].Value = item.First;
                workSheet.Cells[recordIndex, 5].Value = item.FirstSalesTotal;
                workSheet.Cells[recordIndex, 6].Value = item.SellerFirst;
             
                recordIndex ++;
            }
            workSheet.Column(1).AutoFit();
            workSheet.Column(2).AutoFit();
            workSheet.Column(3).AutoFit();
            workSheet.Column(4).AutoFit();
            workSheet.Column(5).AutoFit();
            workSheet.Column(6).AutoFit();
   
            var fileBytes = await excel.GetAsByteArrayAsync();
            using var streamRef = new DotNetStreamReference(stream:new MemoryStream(fileBytes));
            await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"Kampanya_Raporu.xlsx", streamRef);
        } catch(Exception e){
            Console.WriteLine(e);
        }
    }
}
