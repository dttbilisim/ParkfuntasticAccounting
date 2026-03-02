using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Dtos.CompanyRateDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components.Web;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.TierDto;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertCompanyRate
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

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
        public ICompanyRateService CompanyRateService { get; set; }

        [Inject]
        public ICompanyService CompanyService { get; set; }

        [Inject]
        public IProductService ProductService { get; set; }

        [Inject]
        public ICategoryService CategoryService { get; set; }

        [Inject]
        public ITierService TierService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Parameter]
        public int? Id { get; set; }

        [Parameter]
        public int CompanyId { get; set; }

        private bool ShowProducts = false;
        private bool ShowCategories = false;
        private bool ShowTiers = false;



        protected bool errorVisible;
        private bool IsSaveButtonDisabled = true;
        private bool Status = true;

        private string SelectedTierType = "Seçiniz";
        private List<string> TierType = new() { "Seçiniz", "Ürün", "Kategori", "Ürün Grubu" };

        protected CompanyRateUpsertDto companyRate = new();
        public List<ProductListDto> Products { get; set; }
        public List<CategoryListDto> Categories { get; set; }
        public List<TierListDto> Tiers { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var productResponse = await ProductService.GetProducts();
            var categoryResponse = await CategoryService.GetCategories();
            var tierResponse = await TierService.GetTiers();


            if (productResponse.Ok)
                Products = productResponse.Result;

            if (categoryResponse.Ok)
                Categories = categoryResponse.Result;

            if (tierResponse.Ok)
                Tiers = tierResponse.Result;

            if (Id.HasValue)
            {
                var companyRateResponse = await CompanyRateService.GetCompanyRateById(Id.Value);
                if (companyRateResponse.Ok)
                {
                    companyRate = companyRateResponse.Result;
                    Status = companyRate.Status == (int)EntityStatus.Passive || companyRate.Status == (int)EntityStatus.Deleted ? false : true;

                    if (companyRate.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;

                    if (companyRate.ProductId != null)
                        SelectedTierType = "Ürün";
                    else if(companyRate.CategoryId != null)
                        SelectedTierType = "Kategori";
                    else if(companyRate.TierId != null)
                        SelectedTierType = "Ürün Grubu";

                    ChangeEvent(SelectedTierType);

                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, companyRateResponse.GetMetadataMessages());
            }
        }

        protected async Task FormSubmit()
        {
            var validationStatus = await CustomValidation();
            if (validationStatus.Item1)
            {
                try
                {
                    companyRate.Id = Id;
                    companyRate.CompanyId = CompanyId;
                    companyRate.StatusBool = Status;


                    var submitRs = await CompanyRateService.UpsertCompanyRate(new Core.Helpers.AuditWrapDto<CompanyRateUpsertDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = companyRate
                    });

                    if (submitRs.Ok)
                        DialogService.Close(companyRate);
                    else
                        NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());

                }
                catch (Exception ex)
                {
                    errorVisible = true;
                    NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
                }
            }
            else
                await DialogService.Alert(validationStatus.Item2, "Uyarı",new AlertOptions() { OkButtonText="Tamam"});
           
        }

        protected async Task<(bool,string)> CustomValidation()
        {
            if (companyRate.ProductId == null && companyRate.CategoryId == null && companyRate.TierId == null)
                return (false,"Aşama Tipi Giriniz");
            else if (companyRate.Rate == null)
                return (false,"Oran Giriniz");
            return (true,"");
        }


        protected void CancelButtonClick(MouseEventArgs args)
        {
            DialogService.Close(null);
        }

        protected void ChangeEvent(object e)
        {
            var currentValue = e.ToString();

            companyRate.ProductId = null;
            companyRate.CategoryId = null;
            companyRate.TierId = null;

            IsSaveButtonDisabled = currentValue == "Seçiniz";
            ShowCategories = currentValue == "Kategori";
            ShowProducts = currentValue == "Ürün";
            ShowTiers = currentValue == "Ürün Grubu";
        }
    }
}
