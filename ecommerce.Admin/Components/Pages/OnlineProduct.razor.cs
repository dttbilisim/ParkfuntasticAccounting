using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages;
public partial class OnlineProduct{
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] public ICompanyService CompanyService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] public IFileService FileService{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    [Inject] public IProductService _productService{get;set;}
    protected List<ProductOnlineDto> searchResponses = null;
    protected List<ProductListDto> _productListDtos;
    protected RadzenDataGrid<ProductOnlineDto> ? radzenDataGrid = new();
    [Inject] public IJSRuntime _JsRuntime{get;set;}
    private bool IsShowLoadingBar;
    private int count;
    private PageSetting pager;
    private async Task LoadData(LoadDataArgs args){
        var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id" : args.OrderBy.Replace("np", "");
        args.Filter = args.Filter.Replace("np", "");
        pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
        await GetData();
        // var products = await _productService.GetProducts();
        // if(products.Ok){
        //     _productListDtos = products.Result;
        // }
    }
    private async Task GetData(){
        IsShowLoadingBar = true;
        var data = await _productService.GetProductOnline(pager);
        searchResponses = data.Result.Data;
        count = data.Result.DataCount;
        StateHasChanged();
        IsShowLoadingBar = false;
    }
    // void RowRender(RowRenderEventArgs<ProductOnlineDto> args){
    //     if(_productListDtos != null){
    //         foreach(var product in _productListDtos.DistinctBy(x=>x.Barcode).Where(x =>args.Data.Barcodes.Count()>0 && args.Data.Barcodes.Contains(x.Barcode))){
    //             args.Attributes.Add("style", $"background-color: #D0F0C0;");
    //         }
    //     }
    // }
     public async Task ExcelExportClick(){

         IsShowLoadingBar = true;
            var excelFileUrl = string.Empty;
           
            pager = new PageSetting(null,null, 0,int.MaxValue,true);
            var data = await _productService.GetProductOnline(pager);
            searchResponses = data.Result.Data;


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
                workSheet.Cells[1, 3].Value = "Barkod";
                workSheet.Cells[1, 4].Value = "Psf";
                workSheet.Cells[1, 5].Value = "Min.Fiyat";
                workSheet.Cells[1, 6].Value = "Max.Fiyat";


                var recordIndex = 2;
                foreach (var item in searchResponses.ToList())
                {
                    workSheet.Cells[recordIndex, 1].Value = item.Name;
                    workSheet.Cells[recordIndex, 2].Value = item.Brand;
                    workSheet.Cells[recordIndex, 3].Value = item.Barcodes;
                    workSheet.Cells[recordIndex, 4].Value = item.Psf.Value.ToString("n");
                    workSheet.Cells[recordIndex, 5].Value = item.MinPrice.Value.ToString("n");
                    workSheet.Cells[recordIndex, 6].Value = item.MaxPrice.Value.ToString("n");

                    recordIndex++;
                }
                workSheet.Column(1).AutoFit();
                workSheet.Column(2).AutoFit();
                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                workSheet.Column(6).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
               
                await _JsRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_ProductOnline.xlsx", streamRef);
                IsShowLoadingBar = false;
            }
            catch (Exception e)
            {
                IsShowLoadingBar = false;
                Console.WriteLine(e);

            }

        }
}
