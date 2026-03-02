using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Dtos.ProductCategory;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductCategory
    {
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

        [Inject]
        public IProductCategoryService ProductCategoryService { get; set; }

        [Inject]
        public ICategoryService CategoryService { get; set; }

        [Inject]
        public IConfiguration Configuration { get; set; }

        [Inject]
        public IAppSettingService AppSettingService { get; set; }
         
        [Inject]
        protected AuthenticationService Security { get; set; }

        [Parameter]
        public int ProductId { get; set; }


        protected bool errorVisible;
        protected ProductCategoryUpsertDto productCategory = new();
        protected List<CategoryListDto> Categories { get; set; }


        protected override async Task OnInitializedAsync()
        {
            var categoryResponse = await CategoryService.GetTreeCategories();
            if (categoryResponse.Ok)
                Categories = categoryResponse.Result;
        }

        protected async Task FormSubmit()
        {
            try
            {
                productCategory.ProductId = ProductId;
                
                // CategoryId değeri varsa Categories listesine dönüştür
                if (productCategory.CategoryId > 0)
                {
                    productCategory.Categories = new List<int> { productCategory.CategoryId };
                }
                else
                {
                    // Eğer kategori seçilmemişse hata ver
                    NotificationService.Notify(NotificationSeverity.Error, "Lütfen bir kategori seçiniz.");
                    return;
                }

                var submitRs = await ProductCategoryService.UpsertProductCategory(new Core.Helpers.AuditWrapDto<ProductCategoryUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = productCategory
                });
                
                if (submitRs.Ok)
                {
                    DialogService.Close(productCategory);
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

        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }


    }
}
