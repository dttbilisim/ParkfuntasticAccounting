using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using ecommerce.Admin.Helpers.Concretes;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductImage
    {
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] public IProductImageService Service { get; set; }
        [Inject] public IConfiguration Configuration { get; set; }
        [Inject] public IWebHostEnvironment Environment { get; set; }
        [Inject] public IAppSettingService AppSettingService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] public IFileService FileService { get; set; }
        [Parameter] public int? Id { get; set; }
        [Parameter] public int ProductId { get; set; }
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 1024 * 1024 * 5;
        protected int maxAllowedFiles = 5;
        protected int productImageMaxOrderNumber = 1;
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected ProductImageUpsertDto productImage = new();
        public bool Status { get; set; } = true;
        protected override async Task OnInitializedAsync()
        {
            await GetAppSettings();
            if (Id.HasValue)
            {
                var response = await Service.GetProductImage(Id.Value);
                if (response.Ok)
                {
                    productImage = response.Result;
                    Status = productImage.Status == EntityStatus.Passive || productImage.Status == EntityStatus.Deleted ? false : true;
                    if (productImage.Status == EntityStatus.Deleted) IsSaveButtonDisabled = true;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            else
            {
                var productImageOrderNumberResponse = await Service.GetProductImageMaxOrderNumber(ProductId);
                if (productImageOrderNumberResponse.Ok) productImageMaxOrderNumber = productImageOrderNumberResponse.Result;
                productImage.Order = productImageMaxOrderNumber;
            }
        }
        protected async Task FormSubmit()
        {
            try
            {
                productImage.Id = Id;
                productImage.ProductId = ProductId;
                productImage.StatusBool = Status;
                var submitRs = await Service.UpsertProductImage(new Core.Helpers.AuditWrapDto<ProductImageUpsertDto>() { UserId = Security.User.Id, Dto = productImage });
                if (submitRs.Ok)
                {
                    DialogService.Close(productImage);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
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
                if (!item.ContentType.Contains("image/webp") && !item.ContentType.Contains("image/png") && !item.ContentType.Contains("image/jpeg") && !item.ContentType.Contains("image/jpg") && !item.ContentType.Contains("image/jfif"))
                {
                    NotificationService.Notify(NotificationSeverity.Error, "Dosya yüklenirken hata oluştu lütfen uygun formatta dosya yükleyiniz.");
                    return;
                }

               
                try
                {
                    var newFileGuid = await PrepareUniqueImageName(item);
                    var newFileName = await PrepareUniqueImageName(item);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "ProductImages", newFileGuid); /*await GetDirectoryPathByKey("ProductStorageDirectory", newFileName);*/
                    productImage.FileName = item.Name;
                    productImage.FileGuid = newFileGuid;
                    productImage.Root = path;                   
                    var itemStream = resized.OpenReadStream(10000000);
                    if (productImage.FileName.ToLower().Contains("png"))
                    {
                         await FileService.CompressImage(itemStream, "png", path,true,true);
                    }
                    else if (productImage.FileName.ToLower().Contains("jpg") || productImage.FileName.ToLower().Contains("jpeg"))
                    {
                         await FileService.CompressImage(itemStream, "jpg",path,true,true);
                    }
                    else if (productImage.FileName.ToLower().Contains("webp"))
                        {
                            await FileService.CompressImage(itemStream, "webp",path,true,true);
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
        protected void CancelButtonClick(MouseEventArgs args) { DialogService.Close(null); }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item)
        {
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task DirectoryControl()
        {
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "ProductImages");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        private async Task<string> GetDirectoryPathByKey(string key, string newFileName)
        {
            var path = Path.Combine(Configuration.GetValue<string>(key)!, newFileName);
            return path;
        }
        private async Task GetAppSettings()
        {
            var imageUploadLimitResponse = await AppSettingService.GetValues("ProductImageUploadLimit", "ProductImageUploadResizeDimension");
            if (imageUploadLimitResponse.Ok)
            {
                appSettingUploadFileSize = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadLimit")?.Value);
                imageResizeWidth = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadResizeDimension")?.Value.Split("x")[0]);
                imageResizeHeight = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ProductImageUploadResizeDimension")?.Value.Split("x")[1]);
                MaxFileSize = 1024 * 1024 * appSettingUploadFileSize;
            }
        }
    }
}
