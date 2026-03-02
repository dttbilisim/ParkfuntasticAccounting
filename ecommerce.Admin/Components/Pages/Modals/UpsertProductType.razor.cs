using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductTypeDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertProductType
    {
        #region Injections
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
        public IProductTypeService ProductTypeService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected ProductTypeUpsertDto productType = new();
        public bool Status { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            if (Id.HasValue)
            {
                var response = await ProductTypeService.GetProductTypeById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    productType = response.Result;
                    Status = productType.Status == (int)EntityStatus.Passive || productType.Status == (int)EntityStatus.Deleted ? false : true;

                    if (productType.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                productType.Id = Id;
                productType.StatusBool = Status;

                var submitRs = await ProductTypeService.UpsertProductType(new Core.Helpers.AuditWrapDto<ProductTypeUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = productType
                });
                if (submitRs.Ok)
                {

                    DialogService.Close(productType);
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
