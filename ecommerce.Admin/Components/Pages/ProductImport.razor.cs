using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using OfficeOpenXml;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages;
public partial class ProductImport{
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] public IProductService _productService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] protected ecommerce.Admin.Domain.Services.IPermissionService PermissionService{get;set;}
    private const string MENU_NAME = "product-import";
    [Inject] public IFileService FileService{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    protected List<ProductTransactionDto> pharmacyDatas = new();
    protected RadzenDataFilter<ProductTransactionDto> ? dataFilter;
    int count;
    private int transCount = 1;
    private int transTotal = 0;
    private string transName;
    private string resultStyle;
    private PageSetting pager = new();
    public string ProductName{get;set;}
    protected RadzenDataGrid<ProductTransactionDto> ? radzenDataGrid = new();
    protected override async Task OnInitializedAsync(){await GetData();}
    private async Task GetData(){
        if (!await PermissionService.CanView(MENU_NAME))
        {
             NotificationService.Notify(NotificationSeverity.Error, "Görüntüleme yetkiniz bulunmamaktadır.");
             return;
        }
        var pharmacyData = await _productService.GetProductsImportList(pager);
        if(pharmacyData.Result != null){
            pharmacyDatas = pharmacyData.Result.Data.ToList();
            count = pharmacyData.Result.DataCount;
            StateHasChanged();
        } else
            if(pharmacyData.Exception != null){
                NotificationService.Notify(NotificationSeverity.Error, pharmacyData.GetMetadataMessages());
            }
    }
    private async Task LoadData(LoadDataArgs args){
        var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
        args.Filter = args.Filter.Replace("np", "");
        pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
        await GetData();
    }
    void RowRender(RowRenderEventArgs<PharmacyData> args){
        if(args.Data.Status == (int) EntityStatus.Passive)
            args.Attributes.Add("style", $"background-color: #FFEFEF;");
        else
            if(args.Data.Status == (int) EntityStatus.Deleted) args.Attributes.Add("style", $"background-color: #FFE1E1;");
    }
    protected async Task LoadFiles(InputFileChangeEventArgs e){
        if (!await PermissionService.CanCreate(MENU_NAME))
        {
             NotificationService.Notify(NotificationSeverity.Error, "Oluşturma/Aktarım yetkiniz bulunmamaktadır.");
             return;
        }
        foreach(var file in e.GetMultipleFiles(1)){
            try{
                using var ms = new MemoryStream();
                await file.OpenReadStream(10000000).CopyToAsync(ms);
                ms.Position = 0;
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var excelPackage = new ExcelPackage(ms);
                var worksheet = excelPackage.Workbook.Worksheets.FirstOrDefault();
                var totalColumn = worksheet.Dimension.End.Column;
                var totalRows = worksheet.Dimension.Rows;
                transTotal = totalRows;
                if(await DialogService.Confirm("Toplam :" + totalRows + " Kayıt var bu işlem uzun sürecektir.Lütfen işlem bitinceye kadar ekranı kapatmayınız.", "Evet", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) != true) continue;
                for(var row = 2; row <= totalRows; row ++){
                    var ps = new ProductTransaction();
                    for(var col = 1; col <= totalColumn; col ++){
                        switch(col){
                            case 1:
                                if(worksheet.Cells[row, col].Value.ToString() == null){
                                    NotificationService.Notify(NotificationSeverity.Error, "Barkod alanı boş olan ürünler var.Barkod alanları dolu olmalıdır.Lütfen Kontrol ediniz.");
                                    return;
                                } else{
                                    ps.Product = worksheet.Cells[row, col].Value.ToString();
                                    break;
                                }
                            case 2:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                ps.Category = worksheet.Cells[row, col].Value.ToString();
                                break;
                            case 3:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                 
                                ps.SubCategory1 = worksheet.Cells[row, col].Value.ToString()??null;
                                break;
                               
                            case 4:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                
                                ps.SubCategory2 = worksheet.Cells[row, col].Value.ToString()??null;
                                break;
                            case 5:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                ps.Manufacturer = worksheet.Cells[row, col].Value.ToString();
                                break;
                            case 6:
                                ps.Barcode = worksheet.Cells[row, col].Value.ToString()!;
                                break;
                            case 7:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                ps.Form = worksheet.Cells[row, col].Value.ToString() ?? null;
                                break;
                            case 8:
                                if (worksheet.Cells[row, col].Value==null)continue;
                                ps.Tax =Convert.ToInt32(worksheet.Cells[row, col].Value);
                                break;
                          
                            
                            case 9:
                                ps.Length = Convert.ToDecimal(worksheet.Cells[row, col].Value.ToString().Replace(".",","));
                                break;
                            case 10:
                                ps.Height = Convert.ToDecimal(worksheet.Cells[row, col].Value.ToString().Replace(".",","));
                                break;
                            case 11:
                                ps.Width = Convert.ToDecimal(worksheet.Cells[row, col].Value.ToString().Replace(".",","));
                                break;
                            case 12:
                                ps.ReatilPrice = Convert.ToDecimal(worksheet.Cells[row, col].Value);
                                break;
                        }
                        if(col == 1) ProductName = worksheet.Cells[row, col].Value.ToString();
                    }
                    var rs = await _productService.UpsertProductImport(ps);
                    transCount ++;
                    transName = ps.Product;
                    resultStyle = rs.Result;
                    
                    StateHasChanged();
                }
                await GetData();
                NotificationService.Notify(NotificationSeverity.Success, "Aktarım Tamamlandı.");
            } catch(Exception ex){
                transName += " " + ex.Message.ToString() + ProductName + " isimli urunde bir hata olustu";
                NotificationService.Notify(NotificationSeverity.Warning, ex.Message);
            }
        }
    }
}
