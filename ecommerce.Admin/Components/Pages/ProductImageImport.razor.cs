using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Radzen;
namespace ecommerce.Admin.Components.Pages{
    public partial class ProductImageImport{
        [Inject] public IProductService productService{get;set;}
        [Inject] public IProductImageService productImageService{get;set;}
        [Inject] public IFileService FileService{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 1024 * 1024 * 5;
        protected int maxAllowedFiles = 100;
        protected int productImageMaxOrderNumber = 1;
        protected bool errorVisible;
        protected ProductUpsertDto product;
        protected ProductImageUpsertDto productImage = new();
        private async Task LoadFiles(InputFileChangeEventArgs e){
            var varproduct = 0;
            var yokproduct = 0;
            string ? yokProductList = null;
            foreach(var item in e.GetMultipleFiles(maxAllowedFiles)){
                var barcode = Path.GetFileNameWithoutExtension(item.Name);
                var newFileName = "";
                try
                {
                  

                    var resized = item;
                    if (!item.ContentType.Contains("image/webp") && !item.ContentType.Contains("image/png") && !item.ContentType.Contains("image/jpeg") && !item.ContentType.Contains("image/jpg"))
                    {
                        NotificationService.Notify(NotificationSeverity.Error, "Dosya yüklenirken hata oluştu lütfen uygun formatta dosya yükleyiniz.");
                        continue;
                    }

                    var newFileGuid = await PrepareUniqueImageName(item);
                    newFileName = await PrepareUniqueImageName(item);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "ProductImages", newFileGuid); 

                   
                    var data = await productService.GetProductByBarcode(barcode.Trim());
                    product = data.Result;

                    if (product != null && product.Id != null && product.Id != 0)
                    {
                        var itemStream = resized.OpenReadStream(10000000);
                        
                        productImage.FileName = item.Name;
                        productImage.FileGuid = newFileGuid;
                        productImage.Root = path;
                        productImage.ProductId = (int)product.Id;
                        productImage.Status = EntityStatus.Active;
                        productImage.StatusBool = true;
                        productImage.Order = 1;

                        message = "Resimler yukleniyor..Lütfen bekleyiniz....";

                        if (productImage.FileName.ToLower().Contains("png"))
                        {
                            await FileService.CompressImage(itemStream, "png", path, true,true);
                        }
                        else if (productImage.FileName.ToLower().Contains("jpg") || productImage.FileName.ToLower().Contains("jpeg"))
                            {
                                await FileService.CompressImage(itemStream, "jpg", path, true,true);
                            }
                            else if (productImage.FileName.ToLower().Contains("webp"))
                            {
                                await FileService.CompressImage(itemStream, "webp", path, true,true);
                            }
                       

                        await productImageService.UpsertProductImage(new Core.Helpers.AuditWrapDto<ProductImageUpsertDto>() { UserId = 1, Dto = productImage });

                        varproduct++;

                    }
                    else
                    {
                        yokproduct++;
                        yokProductList += yokproduct + "-" + barcode + "; ";
                    }
                }
                catch (Exception)
                {
                    yokproduct++;
                    yokProductList += yokproduct + "-" + barcode + "; ";
                    continue;
                }
                
            }
            var resultmessage = string.Empty;
            resultmessage = varproduct + " resim basari ile duzenlenmistir. " + yokproduct + " resmin ürün bilgileri bulunamadi " + yokProductList;
            message = resultmessage;

            //NotificationService.Notify(NotificationSeverity.Success, "", resultmessage, 5000);
        }
        private void List<T>(){throw new NotImplementedException();}
        private async Task DirectoryControl(){
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "ProductImages");
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            //string newFileName = Path.GetFileName(item.Name);   
            return newFileName;
        }
    }
}
