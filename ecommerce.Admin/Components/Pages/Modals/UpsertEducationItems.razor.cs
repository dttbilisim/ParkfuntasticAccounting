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
using Nest;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertEducationItems{
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
    [Parameter] public int Id{get;set;}
    private bool IsSaveButtonDisabled = false;
    protected bool errorVisible;
    protected EducationItemsUpsertDto educationUpsert = new();
    protected int imageResizeWidth = 500;
    protected int imageResizeHeight = 500;
    protected int appSettingUploadFileSize;
    protected long MaxFileSize = (1024 * 1024) * 25;
    protected int maxAllowedFiles = 5;
    protected bool IsShowLoadingBar = true;
    public bool IsShowLoadingFile = true;
    protected bool IsBannerSaved;
    private new DialogOptions SubItemDialogOptions = new(){Width = "1200px"};
    protected RadzenDataGrid<EducationItemsListDto> ? grid1 = new();
    protected RadzenDataGrid<EducationImagesListDto> ? radzenDataGridProductImage = new();
    protected List<EducationListDto> educationList = null;
    protected List<EducationImagesListDto> productImages = new();
    [Inject] private FileHelper FileHelper{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    public bool Status{get;set;} = true;

    public bool bClearInputFile;
    public string ImageUrlForValidation { get; set; }

    protected override async Task OnInitializedAsync(){
        if(Id > 0){
            var data = await _educationService.GetEducationItemId(Convert.ToInt32(Id));
            if(data.Ok){
                educationUpsert = data.Result;
                Status = educationUpsert.Status == 1 ? true : false;
            }
        }
        await GetEducationList();
        await LoadDataProductImages(Id);
    }
    private async Task GetEducationList(){
        try{
            var rs = await _educationService.GetEducation();
            if(rs.Ok){
                educationList = rs.Result;
            }
            StateHasChanged();
        } catch(Exception e){
            NotificationService.Notify(NotificationSeverity.Warning, "Hata oluştu lütfen tekrar deneyiniz.");
        }
    }
    protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
    protected async void BannerItemTypeChange(object args){
        var count = await _educationService.GetEducationItemLastCount();
        educationUpsert.Order = count;
        educationUpsert.EducationId = (int) args;
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
    private async Task SaveImageFile(IBrowserFile file)
    {
        var fileResponse = await FileService.UploadFile(file, "EducationContent");
        if (!fileResponse.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Error, fileResponse.GetMetadataMessages());
            return;
        }
        else
        {
            EducationImagesUpsertDto img = new();
            img.Status = 1;
            img.ItemUrl = fileResponse.Result.Root;
            img.Order = await _educationService.GetEducationItemImageLastCount();
            img.EducationItemId = (int)Id;

            var rs = await _educationService.UpsertEducationItemImage(new AuditWrapDto<EducationImagesUpsertDto>() { UserId = Security.User.Id, Dto = img }, new AuditWrapDto<EducationItemsUpsertDto>() { UserId = Security.User.Id, Dto = educationUpsert });

            if (rs.Ok)
            {
                Id = img.EducationItemId;
                await LoadDataProductImages(Id);
                NotificationService.Notify(NotificationSeverity.Success, "Fotoğraf Yüklendi. Kayıt Yapıldı.");

                ImageUrlForValidation = "Fotoğraf Yüklendi. Kayıt Yapıldı.";
                StateHasChanged();
                await JSRuntime.InvokeVoidAsync("DispatchChangeEvent", "ImageUrlForValidation");
                //DialogService.Close(educationUpsert);               

            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, "Kayıt eklenirken bir hata ile karşılaşıldı. Lütfen zorunlu alanları (Eğitim Adı, Adı, İçerik Detay) alanlarını girerek tekrar deneyiniz.", duration: 5000);
                ImageUrlForValidation = string.Empty;

                ClearInputFile();
                await JSRuntime.InvokeVoidAsync("DispatchChangeEvent", "ImageUrlForValidation");

            }

        }
    }
    protected async Task FormSubmit(){
        try{
            educationUpsert.Id = Id;
            educationUpsert.StatusBool = Status;
            var submitRs = await _educationService.UpsertEducationItem(new AuditWrapDto<EducationItemsUpsertDto>(){UserId = Security.User.Id, Dto = educationUpsert});
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
   
    protected async Task GridDeleteProductImageButtonClick(MouseEventArgs args, EducationImagesListDto productImage){
        try{
            if(await DialogService.Confirm("Seçilen görseli silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await _educationService.DeleteEducationItemImage(new EducationImagesDeleteDto{Id = productImage.Id});
                if(deleteResult != null){
                    //await InitGridSource();
                    await LoadDataProductImages(Id);
                    NotificationService.Notify(NotificationSeverity.Success, "Fotoğraf Silindi.");
                }
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"});
        }
    }
    private async Task LoadDataProductImages(int educationItemsId){
        var response = await _educationService.GetEducationItemImageId(educationItemsId);
        if(response.Ok && response.Result != null){
            productImages = response.Result.ToList();
            if (productImages.Count > 0)
            {
                ImageUrlForValidation = productImages.FirstOrDefault().ItemUrl;
            }
            else
            {
                ImageUrlForValidation = string.Empty;
            }
        } else{
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        StateHasChanged();
        await JSRuntime.InvokeVoidAsync("DispatchChangeEvent", "ImageUrlForValidation");

    }
    void RowRenderForImage(RowRenderEventArgs<EducationImagesListDto> args){
        if(args.Data.Status == (int) EntityStatus.Passive)
            args.Attributes.Add("style", $"background-color: #FFEFEF;");
        else
            if(args.Data.Status == (int) EntityStatus.Deleted) args.Attributes.Add("style", $"background-color: #FFE1E1;");
    }

    private void ClearInputFile()
    {
        bClearInputFile = true;
        StateHasChanged();
        bClearInputFile = false;
        StateHasChanged();
    }
}
