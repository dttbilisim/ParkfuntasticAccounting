using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Radzen;

namespace ecommerce.Admin.Components.Pages;
public partial class DuplicaProduct{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] public IProductService _productService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    int count;
    protected List<DuplicateProductListDto> productData = null;
    protected Radzen.Blazor.RadzenDataGrid<DuplicateProductListDto> ? grid0 = new();
    private PageSetting pager;
    private new DialogOptions DialogOptions = new(){Width = "1200px"};
    private async Task LoadData(LoadDataArgs args){

        args.OrderBy = args.OrderBy.Replace("np", "");
        args.Filter = args.Filter.Replace("np", "");

        var response = await _productService.GetDublicateProductList();
        productData = response == null ? new List<DuplicateProductListDto>() : response.OrderByDescending(x => x.Ilan).ToList();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            args.Filter = Regex.Replace(args.Filter, @"np\((?<PropertyName>[^\)]+)\)", "${PropertyName}");
            productData = productData.AsQueryable().Where(args.Filter).ToList();

        }

        if (!string.IsNullOrEmpty(args.OrderBy))
        {
            args.OrderBy = Regex.Replace(args.OrderBy, @"np\((?<PropertyName>[^\)]+)\)", "${PropertyName}");
            productData = productData.AsQueryable().OrderBy(args.OrderBy).ToList();
        }       

        count = productData.Count;
   
        StateHasChanged();
    }
    protected async Task GridDeleteButtonClick(MouseEventArgs args, string productName){
        try{
            if(await DialogService.Confirm("Seçilen ürünü silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await _productService.GetDuplicateProductDelete(new DuplicateProductDeleteDto{Name = productName});
                if(deleteResult.Ok){
                    NotificationService.Notify(NotificationSeverity.Success, deleteResult.GetMetadataMessages());
                    await grid0.Reload();
                } else
                    NotificationService.Notify(NotificationSeverity.Error, deleteResult.GetMetadataMessages());
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"});
        }
    }
     public async Task ExcelExportClick()
        {
            var excelFileUrl = string.Empty;
   

            pager = new PageSetting(null,null, 0,int.MaxValue,true);
           

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
                workSheet.Cells[1, 1].Value = "Ürün Id";
                workSheet.Cells[1, 2].Value = "Ürün Adı";
                workSheet.Cells[1, 3].Value = "Mükerrer Sayısı";
                workSheet.Cells[1, 4].Value = "İlan Sayısı";
              

                var recordIndex = 2;
                foreach (var item in productData)
                {
                    workSheet.Cells[recordIndex, 1].Value = item.Id;
                    workSheet.Cells[recordIndex, 2].Value = item.ProductName;
                    workSheet.Cells[recordIndex, 3].Value = item.ProductCount;
                    workSheet.Cells[recordIndex, 4].Value = item.Ilan;
           
                    recordIndex++;
                }
                workSheet.Column(1).AutoFit();
                workSheet.Column(2).AutoFit();
                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
                await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"MukerrerUrunListesi.xlsx", streamRef);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }

        }
}
