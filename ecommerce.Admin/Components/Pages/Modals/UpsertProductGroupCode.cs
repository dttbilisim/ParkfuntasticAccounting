using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertProductGroupCode{
    
    
   
    [Inject]
    protected IJSRuntime JSRuntime { get; set; }

    [Inject]
    protected NavigationManager NavigationManager { get; set; }



    [Inject]
    protected TooltipService TooltipService { get; set; }

    [Inject]
    protected ContextMenuService ContextMenuService { get; set; }

    [Inject]
    protected NotificationService NotificationService { get; set; }


    [Inject]
    public IMapper Mapper { get; set; }

    [Inject]
    protected AuthenticationService Security { get; set; }

    [Inject] protected IProductGroupCodeService _productGroupCodeService{get;set;}
    [Inject]
    protected DialogService DialogService { get; set; }

    [Parameter]
    public int? ProductId { get; set; }
    protected ProductGroupCodeUpsertDto productGroupCodeUpsert = new();
    public bool Status { get; set; } = true;
    protected async Task FormSubmit()
    {

        try
        {
            productGroupCodeUpsert.ProductId = ProductId.Value;
          

            var submitRs = await _productGroupCodeService.UpsertProductGroupCde((new Core.Helpers.AuditWrapDto<ProductGroupCodeUpsertDto>()
            {
                UserId = Security.User.Id,
                Dto = productGroupCodeUpsert
            }));
            if (submitRs.Ok)
            {

                DialogService.Close(productGroupCodeUpsert);
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        }
        catch (Exception ex)
        {
           
            NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
        }
    }
    protected void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close(null);
    }
}
