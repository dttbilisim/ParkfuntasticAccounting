using AutoMapper;

using ecommerce.Admin.Domain.Dtos.EducationDto;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertEducationCategory
{
    [Inject] protected IJSRuntime JSRuntime { get; set; }
    [Inject] protected NavigationManager NavigationManager { get; set; }
    [Inject] protected DialogService DialogService { get; set; }
    [Inject] protected TooltipService TooltipService { get; set; }
    [Inject] protected ContextMenuService ContextMenuService { get; set; }
    [Inject] protected NotificationService NotificationService { get; set; }
    [Inject] public IConfiguration Configuration { get; set; }
    [Inject] public IEducationService _educationService { get; set; }
    [Inject]    protected AuthenticationService Security { get; set; }
    [Inject] public IFileService FileService { get; set; }
    [Inject] public IMapper Mapper { get; set; }
    [Inject] public IAppSettingService AppSettingService { get; set; }  
    [Inject]    public IEducationService  educationService { get; set; }
    [Parameter] public int? Id { get; set; }
    private bool IsSaveButtonDisabled = false;
    protected bool errorVisible;
    protected EducationUpsertDto educationUpsert = new();
    protected int imageResizeWidth = 500;
    protected int imageResizeHeight = 500;
    protected int appSettingUploadFileSize;
    protected long MaxFileSize = (1024 * 1024) * 25;
    protected int maxAllowedFiles = 5;
    protected bool IsShowLoadingBar = true;
    public bool IsShowLoadingFile = true;
    protected bool IsBannerSaved;
    protected EducationCategoryUpsertDto educationcategory = new();
    protected List<EducationCategoryListDto> educationcategories = new();
    private new DialogOptions SubItemDialogOptions = new() { Width = "1200px" };
    protected RadzenDataGrid<EducationListDto>? grid1 = new();
    public bool Status { get; set; } = true;
    protected override async Task OnInitializedAsync()
    {

        var categoryRs = await educationService.GetTreeCategories();
        if (categoryRs.Ok && categoryRs.Result != null)
            educationcategories = categoryRs.Result;



        if (Id.HasValue)
        {
            educationcategories.RemoveAll(x => x.Id == Id);

            var categorySingleRs = await educationService.GetCategoryById(Id.Value);
            if (categorySingleRs.Ok && categorySingleRs.Result != null)
            {
                //TODO Kaan bu k�s�m mapper la olacak.
                educationcategory = categorySingleRs.Result;
                educationcategories.RemoveAll(x => x.EducationCategoryType != educationcategory.EducationCategoryType);

                Status = educationcategory.Status == (int)EntityStatus.Passive || educationcategory.Status == (int)EntityStatus.Deleted ? false : true;

                if (educationcategory.Status == EntityStatus.Deleted.GetHashCode())
                    IsSaveButtonDisabled = true;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, categorySingleRs.GetMetadataMessages());
            }
        }
    }

    protected async Task FormSubmitAsync()
    {
        educationcategory.Id = Id;
        educationcategory.StatusBool = Status;
        educationcategory.Name = educationcategory.Name;

        var submitRs = await educationService.UpsertCategory(new Core.Helpers.AuditWrapDto<EducationCategoryUpsertDto>()
        {
            UserId = Security.User.Id,
            Dto = educationcategory
        });
        if (submitRs.Ok)
        {

            DialogService.Close(educationcategory);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
        }
    }
    protected void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close(null);
    }

}



    