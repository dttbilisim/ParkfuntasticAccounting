using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Dtos.BannerSubItemDto;
using ecommerce.Core.Utils.ResultSet;
using AutoMapper;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components.Forms;
using ecommerce.Admin.Helpers.Concretes;

namespace ecommerce.Admin.Components.Pages.Modals {
    public partial class UpsertBannerSubItem {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject]
        public IFileService FileService { get; set; }
        [Inject]
        public IBannerSubItemService Service { get; set; }    
        [Inject]
        public IMapper Mapper { get; set; }
        [Inject] public IAppSettingService AppSettingService { get; set; }
        [Parameter]
        public int? Id { get; set; }
        [Parameter]
        public int? BannerItemId { get; set; }
        [Parameter]
        public bool IsBannerSaved { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected BannerSubItemUpsertDto BannerSubItem = new();      
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 1024 * 1024 * 5;
        protected int maxAllowedFiles = 5;
        protected bool IsShowLoadingBar = true;
       
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync() 
        {
             
            if (Id.HasValue)
            {
                var BannerSubItemSingleRs = await Service.GetBannerSubItemById(Id.Value);
                if (BannerSubItemSingleRs.Ok && BannerSubItemSingleRs.Result!=null)
                {
                    //TODO Kaan bu k�s�m mapper la olacak.
                    BannerSubItem = BannerSubItemSingleRs.Result;
                    Status = BannerSubItem.Status == (int)EntityStatus.Passive || BannerSubItem.Status == (int)EntityStatus.Deleted ? false : true;

                    if (BannerSubItem.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;                  
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, BannerSubItemSingleRs.GetMetadataMessages());
                }
             
            }             
            IsShowLoadingBar = false;
        }

        protected async Task TabChange(int index)
        {

            switch (index)
            {
                case 3:
                    {
                        break;
                    }
                case 4:
                    {
                        break;
                    }
            }

        }
        [Inject]
        protected AuthenticationService Security { get; set; }

        protected async Task FormSubmit() 
        {
            try
            {
                if (IsBannerSaved == false)
                {
                    BannerSubItem.Id = Id;
                    BannerSubItem.StatusBool = Status;
                    BannerSubItem.BannerItemId = BannerItemId.Value;
                    //BannerSubItem.IsMainSlider=BannerSubItem.ParentId == null ? BannerSubItem.IsMainSlider : false;
                    //BannerSubItem.IsMainPage = BannerSubItem.ParentId == null ? BannerSubItem.IsMainPage : false;

                    var submitRs = await Service.UpsertBannerSubItem(new Core.Helpers.AuditWrapDto<BannerSubItemUpsertDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = BannerSubItem
                    });
                    if (submitRs.Ok)
                    {

                        DialogService.Close(BannerSubItem);
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                    }
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Lütfen ilk önce Banner öğe sini kayıt edin");
                }
                
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
           
            foreach (var item in e.GetMultipleFiles(maxAllowedFiles))
            {
                var resized = item;
                //var resized = await item.RequestImageFileAsync(item.ContentType, imageResizeWidth, imageResizeHeight);
                if (resized.Size > MaxFileSize)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Uyarı", "Dosya Boyut Sınırını Aştınız. Dosya Boyutu " + appSettingUploadFileSize + "MB Olmalıdır.", 5000);
                    return;
                }
                try
                {
                    var newFileName = await PrepareUniqueImageName(item);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BannerSubItemImages", newFileName); /*await GetDirectoryPathByKey("ProductStorageDirectory", newFileName);*/
                    BannerSubItem.FileName = newFileName;                 
                    BannerSubItem.Root = path;
                    BannerSubItem.FileGuid = Guid.NewGuid().ToString();                   
                    var itemStream = resized.OpenReadStream();
                    if (BannerSubItem.FileName.ToLower().Contains("png"))
                    {
                         await FileService.CompressImage(itemStream, "png",path,false,false);
                    }
                    if (BannerSubItem.FileName.ToLower().Contains("webp"))
                    {
                        await FileService.CompressImage(itemStream, "webp", path,false,false);
                    }
                    else if (BannerSubItem.FileName.ToLower().Contains("jpg") || BannerSubItem.FileName.ToLower().Contains("jpeg"))
                    {
                         await FileService.CompressImage(itemStream,"jpg",path,false,false);
                    }
                    else
                    {
                        await using FileStream fs = new(path, FileMode.OpenOrCreate);
                        await itemStream.CopyToAsync(fs);
                    }                                   
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Dosya yüklenirken hata oluştu lütfen tekrar deneyiniz.");
                }
            }
        }

        protected void CancelButtonClick(MouseEventArgs args) {
            DialogService.Close(null);
        }

        private async Task<string> PrepareUniqueImageName(IBrowserFile item)
        {
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task DirectoryControl()
        {
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BannerSubItemImages");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        private async Task<string> GetDirectoryPathByKey(string key, string newFileName)
        {
            var path = Path.Combine(Configuration.GetValue<string>(key)!, newFileName);
            return path;
        }
        private async Task GetAppSettings()
        {
            var imageUploadLimitResponse = await AppSettingService.GetValues("BannerImageUploadLimit", "BannerImageUploadResizeDimension");
            if (imageUploadLimitResponse.Ok)
            {
                appSettingUploadFileSize = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadLimit")?.Value);
                imageResizeWidth = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadResizeDimension")?.Value.Split("x")[0]);
                imageResizeHeight = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "BannerImageUploadResizeDimension")?.Value.Split("x")[1]);
                MaxFileSize = 1024 * 1024 * appSettingUploadFileSize;
            }
        }
    }
}