using AutoMapper;
using ecommerce.Admin.Domain.Dtos.EducationDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using NRules.RuleModel.Builders;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertEducation{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    [Inject] public IEducationService _educationService{get;set;}
    [Inject] public IFileService FileService{get;set;}
    [Inject] public IMapper Mapper{get;set;}
    [Inject] public IAppSettingService AppSettingService{get;set;}
    [Parameter] public int ? Id{get;set;}
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
    private new DialogOptions SubItemDialogOptions = new(){Width = "1200px"};
    protected RadzenDataGrid<EducationListDto> ? grid1 = new();
    protected List<EducationCategoryListDto> category = null;

    [Inject] private FileHelper FileHelper{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    public bool Status{get;set;} = true;
    protected override async Task OnInitializedAsync()
    {
        if (Id > 0)
        {
            var data = await _educationService.GetEducationId(Convert.ToInt32(Id));
            if (data.Ok)
            {
                educationUpsert = data.Result;
                Status = educationUpsert.Status == 1 ? true : false;
            }
        }
        else
        {
            educationUpsert.StartDate = DateTime.Now;
            educationUpsert.EndDate = DateTime.Now.AddMonths(1);
        }
        await GetCategoryList(educationUpsert.EducationCategoryType);   
    }
    private async Task GetCategoryList(EducationCategoryType educationUpsert){
        try{
            var rs = await _educationService.GetCategoriesByEducationCategoryType(educationUpsert);
            if(rs.Ok){
                category = rs.Result;
            }
            StateHasChanged();
        } catch(Exception e){
            NotificationService.Notify(NotificationSeverity.Warning, "Hata oluştu lütfen tekrar deneyiniz.");
        }
    }
    protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
    
    protected async void EducationCategoryTypeChange(object args){
        educationUpsert.EducationCategoryType = (EducationCategoryType)args;
        await GetCategoryList(educationUpsert.EducationCategoryType);
        DialogService.Refresh();
    }
    protected async void BannerItemTypeChange(object args){
        var count = await _educationService.GetEducationLastCount();
        educationUpsert.Order = count;
        educationUpsert.CategoryId = (int) args;   

        DialogService.Refresh();
    }
    protected Task TabChange(int index){
        switch(index){
            case 3:{
                break;
            }
            case 4:{
                break;
            }
        }
        return Task.CompletedTask;
    }
    private async Task<string> PrepareUniqueImageName(IBrowserFile item){
        var randomName = Path.GetRandomFileName();
        var extension = Path.GetExtension(item.Name);
        var newFileName = Path.ChangeExtension(randomName, extension);
        return newFileName;
    }
    private async Task DirectoryControl(){
        var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "EducationFile");
        if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
    }
    private async Task SaveImageFile(IBrowserFile file){
        var fileResponse = await FileService.UploadFile(file, "EducationContent");
        if(!fileResponse.Ok){
            NotificationService.Notify(NotificationSeverity.Error, fileResponse.GetMetadataMessages());
            return;
        }
        educationUpsert.ImageUrl = fileResponse.Result.Root;
        StateHasChanged();
        
        await JSRuntime.InvokeVoidAsync("DispatchChangeEvent", "ImageUrl");
    }
    protected async Task FormSubmit(){
        try{
            educationUpsert.Id = Id;
            educationUpsert.StatusBool = Status;

            if (educationUpsert.EducationCategoryType == EducationCategoryType.Course)
            {
                educationUpsert.IsSlider = true;
            }
            
            var submitRs = await _educationService.UpsertEducation(new AuditWrapDto<EducationUpsertDto>(){UserId = Security.User.Id, Dto = educationUpsert});
            if(submitRs.Ok){
                NotificationService.Notify(NotificationSeverity.Success, "Kayıt Yapıldı");
                DialogService.Close(educationUpsert);
            } else{
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        } catch(Exception ex){
            errorVisible = true;
            NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
        }
    }
}
