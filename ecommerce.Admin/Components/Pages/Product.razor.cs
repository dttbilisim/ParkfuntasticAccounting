using ecommerce.Admin.Domain.Dtos.BrandDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.ProductTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Extensions;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;

using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages{
    public partial class Product{
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IProductService Service{get;set;}
        [Inject] public IBrandService BrandService{get;set;}
        [Inject] public IProductTypeService ProductTypeService{get;set;}
        [Inject] public IJSRuntime _JsRuntime{get;set;}

        int count;
        protected List<ProductListDto> products = new();
        protected List<BrandListDto> brands = new();
        protected List<ProductTypeListDto> productTypes = new();
        protected RadzenDataGrid<ProductListDto> radzenDataGrid = new();
        public List<string> EnumList{get;set;}
        private PageSetting pager;
        public string GeneratedFilter{get;set;}
        public string SearchText { get; set; }
        [Inject] protected AuthenticationService Security{get;set;}
        protected async Task AddButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertProduct>("Ürün Ekle/Düzenle", null, new DialogOptions(){Width = "1200px"});
            await radzenDataGrid.Reload();
        }
        protected async Task FilterButtonClick(MouseEventArgs args){
            var DialogServiceRespose = await DialogService.OpenAsync<FilterProduct>("Ürün Filtrele", new Dictionary<string, object>{{"GeneratedFilter", GeneratedFilter}}, new DialogOptions(){Width = "800px"});
            if(DialogServiceRespose != null){
                GeneratedFilter = DialogServiceRespose;
            }
            await radzenDataGrid.Reload();
        }
        protected async Task ClickMergeProduct(MouseEventArgs args){
            var DialogServiceRespose = await DialogService.OpenAsync<UpsertProductMerge>("Ürün Birleştir", null, new DialogOptions(){Width = "1000px"});
            if(DialogServiceRespose != null){
                GeneratedFilter = DialogServiceRespose;
            }
            await radzenDataGrid.Reload();
        }
        protected async Task EditRow(ProductListDto args){
            await DialogService.OpenAsync<UpsertProduct>("Ürün Düzenle", new Dictionary<string, object>{{"Id", args.Id}}, new DialogOptions(){Width = "1200px"});
            await radzenDataGrid.Reload();
        }
        protected async Task GridDeleteButtonClick(MouseEventArgs args, ProductListDto product){
            try{
                if(await DialogService.Confirm("Seçilen ürünü silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await Service.DeleteProduct(new Core.Helpers.AuditWrapDto<ProductDeleteDto>(){UserId = Security.User.Id, Dto = new ProductDeleteDto(){Id = product.Id}});
                    if(deleteResult.Ok){
                        NotificationService.Notify(NotificationSeverity.Success, deleteResult.GetMetadataMessages());
                        await radzenDataGrid.Reload();
                    } else
                        NotificationService.Notify(NotificationSeverity.Error, deleteResult.GetMetadataMessages());
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"});
            }
        }
      
        public async Task ExcelExportClick(){
            var excelFileUrl = string.Empty;
            pager = new PageSetting(null, null, 0, int.MaxValue, true);
            var response = await Service.GetProducts(pager);
            try{
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "Ürün Adı";
                workSheet.Cells[1, 2].Value = "Kategori 1";
                workSheet.Cells[1, 3].Value = "Kategori 2";
                workSheet.Cells[1, 4].Value = "Kategori 3";
                workSheet.Cells[1, 5].Value = "Marka";
                workSheet.Cells[1, 6].Value = "Barkodlar";
                workSheet.Cells[1, 7].Value = "Form";
                workSheet.Cells[1, 8].Value = "Kdv";
                workSheet.Cells[1, 9].Value = "Genişlik";
                workSheet.Cells[1, 10].Value = "Uzunluk";
                workSheet.Cells[1, 11].Value = "Yükseklik";
                workSheet.Cells[1, 12].Value = "Psf";
                workSheet.Cells[1, 13].Value = "Min Fiyat";
                workSheet.Cells[1, 14].Value = "Max Fiyat";
                workSheet.Cells[1, 15].Value = "Ortalama Fiyat";
                workSheet.Cells[1, 16].Value = "Oluşturma Tarihi";
                workSheet.Cells[1, 17].Value = "Durumu";
                workSheet.Cells[1, 18].Value = "Fotoğraf Sayısı";
                workSheet.Cells[1, 19].Value = "İlan Sayısı";
                var recordIndex = 2;
                foreach(var item in response.Result.Data.ToList()){
                    workSheet.Cells[recordIndex, 1].Value = item.Name;
                    workSheet.Cells[recordIndex, 2].Value = item.Category1;
                    workSheet.Cells[recordIndex, 3].Value = item.Category2;
                    workSheet.Cells[recordIndex, 4].Value = item.Category3;
                    workSheet.Cells[recordIndex, 5].Value = item.Brand.Name;
                    workSheet.Cells[recordIndex, 6].Value = item.Barcode;
                    workSheet.Cells[recordIndex, 7].Value = item.Form;
                    workSheet.Cells[recordIndex, 8].Value = item.Kdv ?? 0;
                    workSheet.Cells[recordIndex, 9].Value = item.Width;
                    workSheet.Cells[recordIndex, 10].Value = item.Length;
                    workSheet.Cells[recordIndex, 11].Value = item.Height;
                    workSheet.Cells[recordIndex, 12].Value = item.RetailPrice?.ToString("n");
                    workSheet.Cells[recordIndex, 13].Value = item.MinPrice?.ToString("n");
                    workSheet.Cells[recordIndex, 14].Value = item.MaxPrice?.ToString("n");
                    workSheet.Cells[recordIndex, 15].Value = item.AvgPrice?.ToString("n");
                    workSheet.Cells[recordIndex, 16].Value = item.CreatedDate.ToShortDateString();
                    workSheet.Cells[recordIndex, 17].Value = item.Status.GetDisplayName();
                    workSheet.Cells[recordIndex, 18].Value = item.ProductsImageCount;
                    workSheet.Cells[recordIndex, 19].Value = item.AdvertCount;
                    recordIndex ++;
                }
                workSheet.Column(1).AutoFit();
                workSheet.Column(2).AutoFit();
                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                workSheet.Column(6).AutoFit();
                workSheet.Column(7).AutoFit();
                workSheet.Column(8).AutoFit();
                workSheet.Column(9).AutoFit();
                workSheet.Column(10).AutoFit();
                workSheet.Column(11).AutoFit();
                workSheet.Column(12).AutoFit();
                workSheet.Column(13).AutoFit();
                workSheet.Column(14).AutoFit();
                workSheet.Column(15).AutoFit();
                workSheet.Column(16).AutoFit();
                workSheet.Column(17).AutoFit();
                workSheet.Column(18).AutoFit();
                workSheet.Column(19).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream:new MemoryStream(fileBytes));
                await _JsRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"{DateTime.Now.Year}_{DateTime.Now.Month}_{DateTime.Now.Day}_Product.xlsx", streamRef);
            } catch(Exception e){
                Console.WriteLine(e);
            }
        }
        public async Task LoadData(LoadDataArgs args){
            if(!string.IsNullOrEmpty(GeneratedFilter)){
                args.Filter = string.IsNullOrEmpty(args.Filter) ? GeneratedFilter : args.Filter + " and " + GeneratedFilter;
            }
            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
            pager.Search = SearchText; // Apply smart search
            var productResponse = await Service.GetProducts(pager);
            if(productResponse.Ok && productResponse.Result != null && productResponse.Result.Data != null){
                products = productResponse.Result.Data;
                count = productResponse.Result.DataCount;
            } else
                if(productResponse.Exception != null){
                    NotificationService.Notify(NotificationSeverity.Error, productResponse.GetMetadataMessages());
                }
            StateHasChanged();
        }
        void RowRender(RowRenderEventArgs<ProductListDto> args){
            if(args.Data.IsCustomerCreated == true){
                args.Attributes.Add("style", $"background-color: #D0F0C0;");
            } else{
                switch(args.Data.Status){
                    case EntityStatus.Passive:
                        args.Attributes.Add("style", $"background-color: #FFEFEF;");
                        break;
                    case EntityStatus.Deleted:
                        args.Attributes.Add("style", $"background-color: #FFE1E1;");
                        break;
                }
            }
        }
    }
}
