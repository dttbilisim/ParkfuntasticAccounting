using ecommerce.Admin.Domain.Dtos.StaticPageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Core.Helpers;
namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertStaticPage{
        #region Injections
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IStaticPageService AboutUsService{get;set;}
        [Inject] public IAppSettingService AppSettingService{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] public IWebHostEnvironment Environment{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        [Inject] protected IFileService FileService{get;set;}
        #endregion
        #region Parameters
        [Parameter] public int ? Id{get;set;}
        #endregion
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 1024 * 1024 * 5;
        protected int maxAllowedFiles = 5;
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected StaticPageUpsertDto staticPage = new();
        [Inject] private FileHelper FileHelper{get;set;}
        IEnumerable<StaticPageType> staticPageTypes = Enum.GetValues(typeof(StaticPageType)).Cast<StaticPageType>();
        protected override async Task OnInitializedAsync(){
            await GetAppSettings();
            if(Id.HasValue){
                var response = await AboutUsService.GetAboutUsById(Id.Value);
                if(response.Ok && response.Result != null){
                    staticPage = response.Result;
                    if(staticPage.Status == EntityStatus.Deleted) IsSaveButtonDisabled = true;
                } else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
        }
        protected async Task FormSubmit(){
            staticPage.Id = Id;
            var submitRs = await AboutUsService.UpsertAboutUs(new AuditWrapDto<StaticPageUpsertDto>(){UserId = Security.User.Id, Dto = staticPage});
            if(submitRs.Ok){
                DialogService.Close(staticPage);
            } else{
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(staticPage);}
        private async Task GetAppSettings(){
            var imageUploadLimitResponse = await AppSettingService.GetValues("ImageUploadLimit", "ImageUploadResizeDimension");
            if(imageUploadLimitResponse.Ok){
                appSettingUploadFileSize = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ImageUploadLimit")?.Value);
                imageResizeWidth = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ImageUploadResizeDimension")?.Value.Split("x")[0]);
                imageResizeHeight = Convert.ToInt32(imageUploadLimitResponse.Result.FirstOrDefault(x => x.Key == "ImageUploadResizeDimension")?.Value.Split("x")[1]);
                MaxFileSize = 1024 * 1024 * appSettingUploadFileSize;
            }
        }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task DirectoryControl(){
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "StaticPageImages");
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        protected async Task LoadFiles(InputFileChangeEventArgs e){
          
            var fileResponse = await FileService.UploadFile(e.File, "StaticPageImages");
            if(!fileResponse.Ok){
                NotificationService.Notify(NotificationSeverity.Error, fileResponse.GetMetadataMessages());
                return;
            }
            staticPage.Root = fileResponse.Result.Root;
            staticPage.FileGuid = fileResponse.Result.GuidFileName;
          
        }
    }
}
