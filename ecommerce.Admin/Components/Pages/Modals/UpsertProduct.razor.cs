using Blazored.FluentValidation;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.BrandDto;
using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto;
using ecommerce.Admin.Domain.Dtos.ProductCategory;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Admin.Domain.Dtos.ProductTierDto;
using ecommerce.Admin.Domain.Dtos.ProductTypeDto;
using ecommerce.Admin.Domain.Dtos.TaxDto;
using ecommerce.Admin.Domain.Dtos.TierDto;
using ecommerce.Admin.Domain.Dtos.ProductStockDto;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Admin.Domain.Dtos.UnitDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Resources;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;

using Radzen;
using Radzen.Blazor;
using static ecommerce.Admin.ConfigureValidators.Validations;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProduct
    {
        #region Injections

        [Inject] protected DialogService DialogService { get; set; } = null!;
        [Inject] protected NotificationService NotificationService { get; set; } = null!;
        [Inject] public IProductService Service { get; set; } = null!;
        [Inject] public IBrandService BrandService { get; set; } = null!;
        [Inject] public ITaxService TaxService { get; set; } = null!;
        [Inject] public IProductTypeService ProductTypeService { get; set; } = null!;
        [Inject] public ICategoryService CategoryService { get; set; } = null!;
        [Inject] public IProductActiveArticleService ProductActiveArticleService { get; set; } = null!;
        [Inject] public IProductImageService ProductImageService { get; set; } = null!;
        [Inject] public ITierService TierService { get; set; } = null!;
        [Inject] public IProductCategoryService ProductCategoryService { get; set; } = null!;
        [Inject] public IProductGroupCodeService ProductGroupCodeService { get; set; } = null!;
        [Inject] public IFileService FileService { get; set; } = null!;
        [Inject] public IProductTierService ProductTierService { get; set; } = null!;
        [Inject] public IProductStockService ProductStockService { get; set; } = null!;
        [Inject] public IProductUnitService ProductUnitService { get; set; } = null!;
        [Inject] public IUnitService UnitService { get; set; } = null!;
        [Inject] public IConfiguration Configuration { get; set; } = null!;
        [Inject] protected AuthenticationService Security { get; set; } = null!;
        [Inject] protected IStringLocalizer<Culture_TR> Loc { get; set; } = null!;
    

        #endregion

        #region Parameters

        [Parameter] public int? Id { get; set; }

        #endregion

        protected long MaxFileSize = 1024 * 1024 * 2;
        protected int maxAllowedFiles = 5;
        protected bool IsSaveButtonDisabled = false;
        protected bool IsShowLoadingBar = true;
        public List<string> ValidationErrors = new();
        protected bool errorVisible;
        protected bool IsProductSaved;
        protected bool isShowSaveThenProductPanels = false;
        public List<int> ToBeRecordedCategories = new();
        protected ProductUpsertDto product = new();
        protected List<CategoryListDto> categories = new();
        protected List<BrandListDto> brands = new();
        protected List<TaxListDto> taxes = new();
        protected List<ProductTypeListDto> productTypes = new();
        protected List<ProductActiveArticleListDto> productActiveArticles = new();
        protected List<ProductGroupCodeListDto> productGroupCodeList = new();
        protected List<ProductImageListDto> productImages = new();
        protected List<ProductImageUpsertDto> productFiles = new();
        protected List<ProductCategoryListDto> productCategories = new();
        protected List<ProductTierListDto> productTiers = new();
        protected List<ProductAdvertListDto> productAdvertList = new();
        protected List<ProductSellerItemListDto> productSellerItems = new();
        protected List<ProductStockListDto> productStocks = new();
        protected List<ProductUnitListDto> productUnits = new();
        // Admin ürün tanımı sadeleştirme: Araç uyumlulukları ürün eklemeden kaldırıldı (yorum satırı). DB'den kaldırılmadı.
        // protected List<ProductCompatibleVehicleDto> compatibleVehicles = new();
        protected List<TierListDto> tiers = new();
        protected List<UnitListDto> units = new();
        protected RadzenDataGrid<ProductActiveArticleListDto>? radzenDataGridActiveArticle;
        protected RadzenDataGrid<ProductImageListDto>? radzenDataGridProductImage;
        protected RadzenDataGrid<ProductAdvertListDto>? radzenDataGridProductAdvert;
        protected RadzenDataGrid<ProductSellerItemListDto>? radzenDataGridProductSellerItems;
        protected RadzenDataGrid<ProductStockListDto>? radzenDataGridProductStock;
        protected RadzenDataGrid<ProductUnitListDto>? radzenDataGridProductUnit;
        // Admin ürün tanımı sadeleştirme: Araç uyumlulukları ürün eklemeden kaldırıldı (yorum satırı).
        // protected RadzenDataGrid<ProductCompatibleVehicleDto>? radzenDataGridCompatibleVehicles;
        private FluentValidationValidator? _fluentValidationValidator;
        public bool Status { get; set; } = true;
        public string BaseUrl { get; set; } = "";

        protected override async Task OnInitializedAsync()
        {
            BaseUrl = Configuration.GetValue<string>("FileUrl") + "ProductImages/";
            
            // Paralel yükleme: Tüm dropdown verilerini aynı anda yükle
            var categoriesTask = CategoryService.GetCategories();
            var brandsTask = BrandService.GetBrands();
            var taxesTask = TaxService.GetTaxes();
            var productTypeTask = ProductTypeService.GetProductTypes();
            var tierTask = TierService.GetTiers();
            var unitTask = UnitService.GetUnits();
            
            await Task.WhenAll(categoriesTask, brandsTask, taxesTask, productTypeTask, tierTask, unitTask);
            
            var categoriesResponse = await categoriesTask;
            var brandsResponse = await brandsTask;
            var taxesResponse = await taxesTask;
            var productTypeResponse = await productTypeTask;
            var tierResponse = await tierTask;
            
            if (categoriesResponse.Ok) categories = categoriesResponse.Result;
            if (brandsResponse.Ok) brands = brandsResponse.Result;
            if (taxesResponse.Ok) taxes = taxesResponse.Result;
            if (productTypeResponse.Ok) productTypes = productTypeResponse.Result;
            else NotificationService.Notify(NotificationSeverity.Error, productTypeResponse.GetMetadataMessages()); // Added Error Notification
            if (tierResponse.Ok) tiers = tierResponse.Result;
            
            var unitResponse = await unitTask;
            if (unitResponse.Ok) 
            {
                units = unitResponse.Result;
                
                // Varsayılan birim seçimi
                if (Id.HasValue && product != null && product.UnitId == null && units.Any())
                {
                    // Varsa ilk birimi seç
                    product.UnitId = units.FirstOrDefault()?.Id;
                }
            }
            
            if (Id.HasValue)
            {
                isShowSaveThenProductPanels = true;
                
                // Ürün detayı ve ilgili verileri paralel yükle
                var productTask = Service.GetProductById(Id.Value);
                var activeArticleTask = LoadDataProductActiveArticle(Id.Value);
                var imagesTask = LoadDataProductImages(Id.Value);
                var categoriesTask2 = LoadProductCategories(Id.Value);
                var tiersTask2 = LoadProductTiers(Id.Value);
                var groupCodeTask = LoadProductGroupCode(Id.Value);
                var advertTask = LoadProductAdvert(Id.Value);
                var sellerItemsTask = LoadProductSellerItems(Id.Value);
                var stockTask = LoadProductStocks(Id.Value);
                var productUnitsTask = LoadProductUnits(Id.Value);
                // Admin ürün tanımı sadeleştirme kapsamında araç uyumlulukları yüklenmiyor.
                // var compatibleVehiclesTask = LoadCompatibleVehicles(Id.Value);
                
                await Task.WhenAll(productTask, activeArticleTask, imagesTask, categoriesTask2, tiersTask2, groupCodeTask, advertTask, stockTask, sellerItemsTask, productUnitsTask);
                
                var productSingleRs = await productTask;
                if (productSingleRs.Ok && productSingleRs.Result != null)
                {
                    product = productSingleRs.Result;
                    Status = product.Status == (int)EntityStatus.Passive || product.Status == (int)EntityStatus.Deleted
                        ? false
                        : true;
                    if (product.Status == EntityStatus.Deleted.GetHashCode()) IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, productSingleRs.GetMetadataMessages());

                if (productCategories != null)
                {
                    product.CategoryIds = productCategories.Select(x => x.CategoryId).ToList();
                }
            }
            else
            {
                IsProductSaved = true;
            }

            IsShowLoadingBar = false;
        }

       
        protected async Task FormSubmit()
        {
            try
            {
                product.Id = Id;
                product.StatusBool = Status;
                var submitRs = await Service.UpsertProduct(new Core.Helpers.AuditWrapDto<ProductUpsertDto>()
                    { UserId = Security.User.Id, Dto = product });
                if (submitRs.Ok)
                {
                    if (Id.HasValue)
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Ürün güncellendi.");
                        DialogService.Close(product);
                    }
                    else
                    {
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Ürün başarıyla eklendi.");
                        DialogService.Close(submitRs.Result);
                    }
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

        #region ValidationEvents

        protected async Task ShowErrors()
        {
            var validator = new ProductUpsertDtoDtoValidator();
            var res = validator.Validate(product);
            ValidationErrors.AddRange(res.Errors.Select(x => x.ErrorMessage));
            List<Dictionary<string, string>> error = await PrepareErrorsForWarningModal(ValidationErrors);
            Dictionary<string, object> param = new();
            param.Add("Errors", error);
            await DialogService.OpenAsync<ValidationModal>("Uyari", param);
            ValidationErrors.Clear();
        }

        private async Task<List<Dictionary<string, string>>> PrepareErrorsForWarningModal(List<string> errors)
        {
            List<Dictionary<string, string>> error = new();
            foreach (var errorText in ValidationErrors)
            {
                Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                messageDictionary.Add(errorText.Split("-")[0], errorText.Split("-")[1]);
                error.Add(messageDictionary);
            }

            return error;
        }

        #endregion

        #region DirectoryAndFilesEvents

        private async Task DirectoryControl(string path = "")
        {
            var directoryPath = Path.Combine(Configuration.GetValue<string>("File:BaseUploadUrl")!);
            if (!string.IsNullOrEmpty(path))
                directoryPath = path;

            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }

        private async Task<string> GetDirectoryPathByKey(string key, string newFileName)
        {
            var path = Path.Combine(Configuration.GetValue<string>(key)!, newFileName);
            return path;
        }

        private async Task<string> PrepareUniqueImageName(IBrowserFile item)
        {
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }

        #endregion

        #region ListEvents

        protected async Task LoadFiles(InputFileChangeEventArgs e, string additionalParameter = "")
        {
            var directoryName = "";
            if (!string.IsNullOrEmpty(additionalParameter))
            {
                directoryName = additionalParameter;
            }

            foreach (var item in e.GetMultipleFiles(maxAllowedFiles))
            {
                try
                {
                    var newFileName = await PrepareUniqueImageName(item);
                    if (!string.IsNullOrEmpty(directoryName))
                    {
                        newFileName = Path.Combine(directoryName, newFileName);
                    }

                    var directoryPath = await GetDirectoryPathByKey("File:BaseUploadUrl", directoryName);
                    await DirectoryControl(directoryPath);

                    var path = "";
                    path = await GetDirectoryPathByKey("File:BaseUploadUrl", newFileName);

                    await using FileStream fs = new(path, FileMode.Create);
                    await item.OpenReadStream(MaxFileSize).CopyToAsync(fs);
                    
                    var fileUrl = await GetDirectoryPathByKey("File:BaseUploadHttpUrl", newFileName);

                    if (directoryName == "productDocument1")
                    {
                        product.DocumentUrl = fileUrl; 
                    }
                    else
                    {
                        product.DocumentUrl2 = fileUrl; 
                    }
                    
                }
                catch (Exception)
                {
                }
            }
        }
        private async Task LoadProductAdvert(int productId){
            var response = await Service.GetProductAdvertListById(productId);
            
            if (response.Ok && response.Result != null)
            {
                productAdvertList = response.Result.ToList();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }
        private async Task LoadDataProductActiveArticle(int productId)
        {
            var response = await ProductActiveArticleService.GetProductActiveArticles(productId);
            if (response.Ok && response.Result != null)
            {
                productActiveArticles = response.Result.ToList();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }

        private async Task LoadProductGroupCode(int productId)
        {
            var response = await ProductGroupCodeService.GetProductGroupCodes(productId);
            if (response.Ok && response.Result != null)
            {
                productGroupCodeList = response.Result.ToList();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }

        private async Task LoadDataProductImages(int productId)
        {
            var response = await ProductImageService.GetProductImages(productId);
            if (response.Ok && response.Result != null)
            {
                productImages = response.Result.ToList();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }

        private async Task LoadProductSellerItems(int productId)
        {
            var response = await Service.GetSellerItemsByProduct(productId);
            if (response.Ok && response.Result != null)
            {
                productSellerItems = response.Result.ToList();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }

        protected async Task LoadSellerItemsClick()
        {
            if (!Id.HasValue)
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Önce ürünü kaydedin");
                return;
            }

            await LoadProductSellerItems(Id.Value);
        }

        private async Task LoadProductCategories(int productId)
        {
            var productCategoryResponse = await ProductCategoryService.GetProductCategories(productId);
            if (productCategoryResponse.Ok)
            {
                productCategories = productCategoryResponse.Result.ToList();
                if (product != null)
                {
                    product.CategoryIds = productCategories.Select(x => x.CategoryId).ToList();
                }
            }
            StateHasChanged();
        }

        private async Task LoadProductTiers(int productId)
        {
            var productTierResponse = await ProductTierService.GetProductTiers(productId);
            if (productTierResponse.Ok) productTiers = productTierResponse.Result;
            StateHasChanged();
        }

        private async Task LoadProductStocks(int productId)
        {
            var stockResponse = await ProductStockService.GetStocksByProduct(productId);
            if (stockResponse.Ok)
            {
                productStocks = stockResponse.Result;
                if (radzenDataGridProductStock != null) await radzenDataGridProductStock.Reload();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, stockResponse.GetMetadataMessages());
            }
            StateHasChanged();
        }

        private async Task LoadProductUnits(int productId)
        {
            var response = await ProductUnitService.GetProductUnitsByProductId(productId);
            if (response.Ok && response.Result != null)
            {
                productUnits = response.Result.ToList();
                if (radzenDataGridProductUnit != null) await radzenDataGridProductUnit.Reload();
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }

            StateHasChanged();
        }

        // Admin ürün tanımı sadeleştirme: Araç uyumlulukları ürün eklemeden kaldırıldı (yorum satırı). DB'den kaldırılmadı.
        // private async Task LoadCompatibleVehicles(int productId)
        // {
        //     var response = await Service.GetCompatibleVehicles(productId);
        //     if (response.Ok && response.Result != null)
        //     {
        //         compatibleVehicles = response.Result;
        //     }
        //     else
        //     {
        //         NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        //     }
        //     StateHasChanged();
        // }

        #endregion

        #region ClickEvents

        protected async Task EditRowActiveArticle(ProductActiveArticleListDto args)
        {
            await DialogService.OpenAsync<UpsertProductActiveArticle>("Etken Madde Düzenle",
                new Dictionary<string, object> { { "Id", args.Id }, { "ProductId", Id.Value } });
            await LoadDataProductActiveArticle(Id.Value);
        }
        
        protected async Task EditRowProductStock(ProductStockListDto args)
        {
            await DialogService.OpenAsync<UpsertProductStock>("Stok Düzenle",
                new Dictionary<string, object> { { "Id", args.Id }, { "ProductId", Id.Value } });
            await LoadProductStocks(Id.Value);
        }

        protected async Task EditRowProductImage(ProductImageListDto args)
        {
            await DialogService.OpenAsync<UpsertProductImage>("Ürün Görsel Düzenle",
                new Dictionary<string, object> { { "Id", args.Id }, { "ProductId", Id.Value } });
            await LoadDataProductImages(Id.Value);
        }

        protected async Task EditRowProductUnit(ProductUnitListDto args)
        {
            await DialogService.OpenAsync<UpsertProductUnit>("Birim Düzenle",
                new Dictionary<string, object> { { "Id", args.Id }, { "ProductId", Id.Value } });
            await LoadProductUnits(Id.Value);
        }

        protected async Task GridDeleteProductImageButtonClick(MouseEventArgs args, ProductImageListDto productImage)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen görseli silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductImageService.DeleteProductImage(
                        new Core.Helpers.AuditWrapDto<ProductImageDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductImageDeleteDto() { Id = productImage.Id }
                        });
                    if (deleteResult != null)
                    {
                        //await InitGridSource();
                        await LoadDataProductImages(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"
                });
            }
        }

        protected async Task GridDeleteProductCategoryButtonClick(MouseEventArgs args,
            ProductCategoryListDto productCategory)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kategoriyi silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductCategoryService.DeleteProductCategory(
                        new Core.Helpers.AuditWrapDto<ProductCategoryDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductCategoryDeleteDto() { Id = productCategory.Id }
                        });
                    if (deleteResult != null)
                    {
                        await LoadProductCategories(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"
                });
            }
        }

        protected async Task GridDeleteProductTierButtonClick(MouseEventArgs args, ProductTierListDto productTier)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kayıdı silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductTierService.DeleteProductTier(
                        new Core.Helpers.AuditWrapDto<ProductTierDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductTierDeleteDto() { Id = productTier.Id }
                        });
                    if (deleteResult != null)
                    {
                        await LoadProductTiers(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"
                });
            }
        }
        
        protected async Task GridDeleteProductStockButtonClick(MouseEventArgs args, ProductStockListDto stock)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen stoğu silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductStockService.DeleteStock(
                        new Core.Helpers.AuditWrapDto<ProductStockDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductStockDeleteDto() { Id = stock.Id }
                        });
                    if (deleteResult != null)
                    {
                         await LoadProductStocks(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete stock"
                });
            }
        }

        protected async Task GridDeleteProductUnitButtonClick(MouseEventArgs args, ProductUnitListDto productUnitDto)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen birimi silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductUnitService.DeleteProductUnit(
                        new Core.Helpers.AuditWrapDto<ProductUnitDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductUnitDeleteDto() { Id = productUnitDto.Id }
                        });
                    if (deleteResult != null)
                    {
                        await LoadProductUnits(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete unit"
                });
            }
        }

        protected async Task GridDeleteProductActiveArticleButtonClick(MouseEventArgs args,
            ProductActiveArticleListDto activeArticle)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen etken maddeyi silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductActiveArticleService.DeleteProductActiveArticle(
                        new Core.Helpers.AuditWrapDto<ProductActiveArticleDeleteDto>()
                        {
                            UserId = Security.User.Id,
                            Dto = new ProductActiveArticleDeleteDto() { Id = activeArticle.Id }
                        });
                    if (deleteResult != null)
                    {
                        //await InitGridSource();
                        await LoadDataProductActiveArticle(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"
                });
            }
        }

        protected async Task GridDeleteProductGroupCodeButtonClick(MouseEventArgs args,
            ProductGroupCodeListDto activeGroupCode)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen grup kodunu silmek istediğinize emin misiniz?", "Kayıt Sil",
                        new ConfirmOptions() { OkButtonText = "Evet", CancelButtonText = "Hayır" }) == true)
                {
                    var deleteResult = await ProductGroupCodeService.DeleteProductGroupCode(
                        new Core.Helpers.AuditWrapDto<ProductGroupCodeDeleteDto>()
                        {
                            UserId = Security.User.Id, Dto = new ProductGroupCodeDeleteDto() { Id = activeGroupCode.Id }
                        });
                    if (deleteResult != null)
                    {
                        await LoadProductGroupCode(Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"
                });
            }
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductActiveArticle>("Etken Madde Ekle", parameters);
            await LoadDataProductActiveArticle(Id.Value);
        }

        protected async Task AddGroupCodeModal(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductGroupCode>("Grup Kodu", parameters);
            await LoadProductGroupCode(Id.Value);
        }

        protected async Task AddProductImageButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductImage>("Görsel Ekle", parameters);
            await LoadDataProductImages(Id.Value);
        }
        
        protected async Task AddProductStockButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductStock>("Stok Ekle", parameters);
            await LoadProductStocks(Id.Value);
        }

        protected async Task AddProductCategoryButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductCategory>("Kategori Ekle", parameters,
                new DialogOptions() { Width = "1200px" });
            await LoadProductCategories(Id.Value);
            // DropDown verisini güncelle
            product.CategoryIds = productCategories.Select(x => x.CategoryId).ToList();
        }

        protected async Task AddProductTierButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductTier>("Ürün Grubu Ekle", parameters);
            await LoadProductTiers(Id.Value);
        }

        protected async Task AddProductUnitButtonClick(MouseEventArgs args)
        {
            var parameters = new Dictionary<string, object>();
            parameters.Add("ProductId", Id.Value);
            await DialogService.OpenAsync<UpsertProductUnit>("Birim Ekle", parameters);
            await LoadProductUnits(Id.Value);
            
            // Birim listesini yeniden yükle ve UnitId'yi güncelle
            var unitResponse = await UnitService.GetUnits();
            if (unitResponse.Ok) units = unitResponse.Result;
            
            // Son eklenen birimi seç
            if (units.Any() && product != null)
            {
                var lastUnit = units.OrderByDescending(u => u.Id).FirstOrDefault();
                if (lastUnit != null)
                {
                    product.UnitId = lastUnit.Id;
                }
            }
        }

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
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

        protected void BarcodeControlSync(string value) { _ = InvokeAsync(async () => await BarcodeControl(value)); }
        protected async Task BarcodeControl(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                IsSaveButtonDisabled = false;
                return;
            }
            
            value = value.Trim();
            
            // Düzenleme modunda: Mevcut ürünün barkodu ise kontrol etme
            if (Id.HasValue && product != null && product.Barcode?.Trim() == value)
            {
                IsSaveButtonDisabled = false;
                return;
            }
            
            // Barkod kontrolü: Sadece bu barkodun var olup olmadığını kontrol et
            var barcodeCheck = await Service.GetProductByBarcode(value);
            if (barcodeCheck.Ok && barcodeCheck.Result != null)
            {
                // Düzenleme modunda: Eğer bulunan ürün mevcut ürünün kendisi ise sorun yok
                if (Id.HasValue && barcodeCheck.Result.Id == Id.Value)
                {
                    IsSaveButtonDisabled = false;
                    return;
                }
                
                IsSaveButtonDisabled = true;
                NotificationService.Notify(NotificationSeverity.Error, "Uyarı",
                    "Girilen barkod numarası sistem'de mevcut lütfen farklı bir barkod numarası giriniz!", 5000);
                return;
            }
            
            IsSaveButtonDisabled = false;
        }

        void RowRenderForImage(RowRenderEventArgs<ProductImageListDto> args)
        {
            if (args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", $"background-color: #FFEFEF;");
            else if (args.Data.Status == EntityStatus.Deleted)
                args.Attributes.Add("style", $"background-color: #FFE1E1;");
        }

        #endregion
    }
}