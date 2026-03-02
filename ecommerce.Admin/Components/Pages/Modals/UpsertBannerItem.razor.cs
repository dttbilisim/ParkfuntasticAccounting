using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Dtos.BannerItemDto;
using ecommerce.Core.Utils.ResultSet;
using AutoMapper;
using ecommerce.Core.Utils;
using ecommerce.Admin.Domain.Dtos.BannerDto;
using Microsoft.AspNetCore.Components.Forms;
using ecommerce.Admin.Domain.Dtos.BannerSubItemDto;
using Radzen.Blazor;
using ecommerce.Core.Models;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Entities;

namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertBannerItem{
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] public IBannerItemService Service{get;set;}
        [Inject] public IFileService FileService{get;set;}
        [Inject] public IBannerSubItemService SubItemService{get;set;}
        [Inject] public IBannerService BannerService{get;set;}
        [Inject] public IMapper Mapper{get;set;}
        [Inject] public IAppSettingService AppSettingService{get;set;}
        [Parameter] public int ? Id{get;set;}
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected BannerItemUpsertDto BannerItem = new();
        protected List<BannerListDto> banners = new();
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 50 * 1024 * 1024;
        protected int maxAllowedFiles = 5;
        protected bool IsShowLoadingBar = true;
        public bool IsShowLoadingFile = true;
        protected bool IsBannerSaved;
        private new DialogOptions SubItemDialogOptions = new(){Width = "1200px"};
        protected RadzenDataGrid<BannerSubItemListDto> ? grid1 = new();
        protected List<BannerSubItemListDto> subitems = null;
        private PageSetting pager;
        int count;
        public bool Status{get;set;} = true;
        protected override async Task OnInitializedAsync(){
            var BanneritemRs = await BannerService.GetBanners();
            if(BanneritemRs.Ok && BanneritemRs.Result != null) banners = BanneritemRs.Result;
            if(Id.HasValue){
                var BannerItemSingleRs = await Service.GetBannerItemById(Id.Value);
                if(BannerItemSingleRs.Ok && BannerItemSingleRs.Result != null){
                    BannerItem = BannerItemSingleRs.Result;
                    Status = BannerItem.Status == (int) EntityStatus.Passive || BannerItem.Status == (int) EntityStatus.Deleted ? false : true;
                    if(BannerItem.Status == EntityStatus.Deleted.GetHashCode()) IsSaveButtonDisabled = true;
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, BannerItemSingleRs.GetMetadataMessages());
                }
                IsBannerSaved = false;
                IsShowLoadingFile = false;
            } else{
                BannerItem.StartDate = DateTime.Now;
                BannerItem.EndDate = DateTime.Now.AddMonths(1);
                IsBannerSaved = true;
            }
            IsShowLoadingBar = false;
        }
        protected async Task TabChange(int index){
            switch(index){
                case 3:{
                    break;
                }
                case 4:{
                    break;
                }
            }
        }
        [Inject] protected AuthenticationService Security{get;set;}
        protected async Task FormSubmit(){
            try{

                if(BannerItem.IsVideo){
                    if(BannerItem.VideoUrl != null){
                        BannerItem.Id = Id;
                        BannerItem.StatusBool = Status;
                        var susscesOrder = await Service.GetBannerItemLastCount(BannerItem.BannerId);
                        if(Id == null && BannerItem.Order < susscesOrder){
                            NotificationService.Notify(NotificationSeverity.Error, "Bu sıra da zaten kayıt var");
                        } else{
                            var submitRs = await Service.UpsertBannerItem(new Core.Helpers.AuditWrapDto<BannerItemUpsertDto>(){UserId = Security.User.Id, Dto = BannerItem});
                            if(submitRs.Ok){
                                DialogService.Close(BannerItem);
                            } else{
                                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                            }
                        }
                    } else{
                        NotificationService.Notify(NotificationSeverity.Error, "Video Url Boş Olamaz");
                    }

                } else{
                    if(BannerItem.Root != null){
                        BannerItem.Id = Id;
                        BannerItem.StatusBool = Status;
                        var susscesOrder = await Service.GetBannerItemLastCount(BannerItem.BannerId);
                        if(Id == null && BannerItem.Order < susscesOrder){
                            NotificationService.Notify(NotificationSeverity.Error, "Bu sıra da zaten kayıt var");
                        } else{
                            var submitRs = await Service.UpsertBannerItem(new Core.Helpers.AuditWrapDto<BannerItemUpsertDto>(){UserId = Security.User.Id, Dto = BannerItem});
                            if(submitRs.Ok){
                                DialogService.Close(BannerItem);
                            } else{
                                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                            }
                        }
                    } else{
                        NotificationService.Notify(NotificationSeverity.Error, "Lütfen İmage Yükleyiniz");
                    }
                }

             
            } catch(Exception ex){
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        protected async Task LoadFiles(InputFileChangeEventArgs e,bool mobile){
            IsShowLoadingFile = true;
            foreach(var item in e.GetMultipleFiles(maxAllowedFiles)){
                var resized = item;
               
                try{
                    var newFileName = await PrepareUniqueImageName(resized);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BannerImages", newFileName); /*await GetDirectoryPathByKey("ProductStorageDirectory", newFileName);*/
                 
                    if(mobile){
                        BannerItem.MobileImageUrl = path;
                        BannerItem.FileNameMobile = newFileName;
                    } else{
                        BannerItem.FileName = newFileName;
                        BannerItem.Root = path;
                    }
                    

                    BannerItem.FileGuid = newFileName;
                    var itemStream = resized.OpenReadStream(100000000);
                    if (mobile)
                    {
                        if (BannerItem.FileNameMobile.ToLower().Contains("png"))
                        {
                            await FileService.CompressImage(itemStream, "png", path, false,false);
                        }
                        else if (BannerItem.FileNameMobile.ToLower().Contains("jpg") || BannerItem.FileName.ToLower().Contains("jpeg"))
                        {
                            await FileService.CompressImage(itemStream, "jpg", path, false,false);
                        }
                        else if (BannerItem.FileNameMobile.ToLower().Contains("webp"))
                        {
                            await FileService.CompressImage(itemStream, "webp", path, false,false);
                        }
                        else if (BannerItem.FileNameMobile.ToLower().Contains("gif"))
                        {
                            await FileService.CompressImage(itemStream, "gif", path, false,false);
                        }
                        else
                        {
                            await using FileStream fs = new(path, FileMode.OpenOrCreate);
                            await itemStream.CopyToAsync(fs);
                        }
                    }
                    else
                    {
                        if (BannerItem.FileName.ToLower().Contains("png"))
                        {
                            await FileService.CompressImage(itemStream, "png", path, false,false);
                        }
                        else if (BannerItem.FileName.ToLower().Contains("jpg") || BannerItem.FileName.ToLower().Contains("jpeg"))
                        {
                            await FileService.CompressImage(itemStream, "jpg", path, false,false);
                        }
                        if (BannerItem.FileName.ToLower().Contains("webp"))
                        {
                            await FileService.CompressImage(itemStream, "webp", path, false,false);
                        }
                        else if (BannerItem.FileName.ToLower().Contains("gif"))
                        {
                            await FileService.CompressImage(itemStream, "gif", path, false,false);
                        }
                        else
                        {
                            await using FileStream fs = new(path, FileMode.OpenOrCreate);
                            await itemStream.CopyToAsync(fs);
                        }
                    }
                   
                    IsShowLoadingFile = false;
                } catch(Exception ex){
                    NotificationService.Notify(NotificationSeverity.Warning,ex.Message+" "+ "Dosya yüklenirken hata oluştu lütfen tekrar deneyiniz.");
                }
            }
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
        protected async void BannerItemTypeChange(object args){
            var count = await Service.GetBannerItemLastCount((int) args);
            BannerItem.Order = count;
            DialogService.Refresh();
        }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task DirectoryControl(){
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BannerImages");
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        private async Task<string> GetDirectoryPathByKey(string key, string newFileName){
            var path = Path.Combine(Configuration.GetValue<string>(key)!, newFileName);
            return path;
        }
        private async Task GetAppSettings(){
            var imageUploadLimitResponse = await AppSettingService.GetValues("BannerImageUploadLimit", "BannerImageUploadResizeDimension");
            if(imageUploadLimitResponse.Ok){
                appSettingUploadFileSize = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadLimit")?.Value);
                imageResizeWidth = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadResizeDimension")?.Value.Split("x")[0]);
                imageResizeHeight = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadResizeDimension")?.Value.Split("x")[1]);
                MaxFileSize = 1024 * 1024 * appSettingUploadFileSize;
            }
        }
        protected async Task AddSubItemButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertBannerSubItem>("Banner Alt Öğe Ekle/Düzenle", new Dictionary<string, object>{{"BannerItemId", Id},{"IsBannerSaved", IsBannerSaved}}, SubItemDialogOptions);
            await grid1.Reload();
        }
        protected async Task EditSubItemRow(BannerSubItemListDto args){
            await DialogService.OpenAsync<UpsertBannerSubItem>("Banner Alt Öğe Düzenle", new Dictionary<string, object>{{"Id", args.Id},{"BannerItemId", Id},{"IsBannerSaved", IsBannerSaved}}, SubItemDialogOptions);
            await grid1.Reload();
        }
        private async Task LoadDataSubItem(LoadDataArgs args){
            pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            var response = await SubItemService.GetBannerSubItems(pager, Id ?? 0);
            if(response.Ok && response.Result != null){
                subitems = response.Result.Data.ToList();
                count = response.Result.DataCount;
            } else{
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
        protected async Task GridDeleteSubItemButtonClick(MouseEventArgs args, BannerSubItemListDto Banner){
            try{
                if(await DialogService.Confirm("Seçilen Banner ı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await SubItemService.DeleteBannerSubItem(new Core.Helpers.AuditWrapDto<BannerSubItemDeleteDto>(){UserId = Security.User.Id, Dto = new BannerSubItemDeleteDto(){Id = Banner.Id}});
                    if(deleteResult != null){
                        if(deleteResult != null){
                            if(deleteResult.Ok)
                                await grid1.Reload();
                            else
                                await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                        }
                    }
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete Banner"});
            }
        }
        void RowRenderSubItem(RowRenderEventArgs<BannerSubItemListDto> args){
            if(args.Data.Status == 0) args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
        }
    }
}
