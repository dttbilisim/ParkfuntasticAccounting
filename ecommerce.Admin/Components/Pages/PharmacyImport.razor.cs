using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages;
public partial class PharmacyImport{
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] public ICompanyService CompanyService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] protected ecommerce.Admin.Domain.Services.IPermissionService PermissionService{get;set;}
    private const string MENU_NAME = "pharmacy-import";
    [Inject] public IFileService FileService{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    protected List<PharmacyDataDto> pharmacyDatas = null;
    protected RadzenDataGrid<PharmacyDataDto> ? radzenDataGrid = new();
    protected RadzenDataFilter<PharmacyDataDto> ? dataFilter;
    int count;
    private int transCount = 0;
    private int transTotal = 0;
    private string transName;
    private string resultStyle;
    private PageSetting pager;
    private async Task LoadData(LoadDataArgs args){
        if (!await PermissionService.CanView(MENU_NAME))
        {
             NotificationService.Notify(NotificationSeverity.Error, "Görüntüleme yetkiniz bulunmamaktadır.");
             return;
        }
        var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id" : args.OrderBy.Replace("np", "");
        args.Filter = args.Filter.Replace("np", "");
        pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
        var pharmacyData = await CompanyService.GetPharmacyData(pager);
        if(pharmacyData.Result != null){
            pharmacyDatas = pharmacyData.Result.Data.OrderByDescending(x => x.StatusText).ToList();
            count = pharmacyData.Result.DataCount;
            StateHasChanged();
        } else
            if(pharmacyData.Exception != null){
                NotificationService.Notify(NotificationSeverity.Error, pharmacyData.GetMetadataMessages());
            }
      
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
        foreach(var item in e.GetMultipleFiles()){
            var resized = item;
            try{
                var buffer = new byte[item.Size];
                using var memoryStream = new MemoryStream();
                await item.OpenReadStream(100000000000).CopyToAsync(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                buffer = memoryStream.ToArray();
                var jsonString = System.Text.Encoding.UTF8.GetString(buffer);
                var items = JsonConvert.DeserializeObject<PharmacyTransferDto.Root>(jsonString);
                transTotal = items.aaData.Count;
                if(await DialogService.Confirm("Toplam :" + items.aaData.Count.ToString() + " Kayıt var bu işlem uzun sürecektir.Lütfen işlem bitinceye kadar ekranı kapatmayınız.", "Evet", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    foreach(var data in items.aaData){
                        var pharmacyData = new PharmacyData{
                            PharmacyType = data[1],
                            GlnNumber = data[2],
                            PharmacyName = data[3],
                            Email = data[4],
                            StatusText = data[5],
                            Status = 1,
                            City = data[6],
                            Town = data[7],
                            CreatedDate = DateTime.Now,
                            CreatedId = 1
                        };
                        var rs = await CompanyService.UploadPharmactData(pharmacyData);
                        transCount ++;
                        transTotal --;
                        resultStyle = rs.Result;
                        transName = data[3];
                        StateHasChanged();
                    }
                }
                transTotal = 0;
                transCount = 0;
                transName = string.Empty;
            } catch(Exception ex){
                NotificationService.Notify(NotificationSeverity.Warning,ex.Message);
            }
        }
    }
}
