using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using ecommerce.Admin.Services;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Core.Utils.ResultSet;
using AutoMapper;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Helpers.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Dtos.Options;
using Microsoft.AspNetCore.Components.Forms;
namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertCategory{
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public ICategoryService Service{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] public IMapper Mapper{get;set;}
        [Inject] public IFileService FileService{get;set;}
        [Inject] private CdnOptions CdnConfig{get;set;}
        [Parameter] public int ? Id{get;set;}
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected int imageResizeWidth = 500;
        protected int imageResizeHeight = 500;
        protected int appSettingUploadFileSize;
        protected long MaxFileSize = 50 * 1024 * 1024;
        protected int maxAllowedFiles = 5;
        protected CategoryUpsertDto category = new();
        protected List<CategoryListDto> categories = new();
        public bool Status{get;set;} = true;
        public bool IsShowLoadingFile = true;
        protected override async Task OnInitializedAsync(){
            var categoryRs = await Service.GetTreeCategories();
            if(categoryRs.Ok && categoryRs.Result != null) categories = categoryRs.Result;
            if(Id.HasValue){
                var categorySingleRs = await Service.GetCategoryById(Id.Value);
                if(categorySingleRs.Ok && categorySingleRs.Result != null){
                    category = categorySingleRs.Result;
                    Status = category.Status == (int) EntityStatus.Passive || category.Status == (int) EntityStatus.Deleted ? false : true;
                    if(category.Status == EntityStatus.Deleted.GetHashCode()) IsSaveButtonDisabled = true;
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, categorySingleRs.GetMetadataMessages());
                }
            }
        }
        [Inject] protected AuthenticationService Security{get;set;}
        protected async Task FormSubmit(){
            try{
                category.Id = Id;
                category.StatusBool = Status;
                category.IsMainSlider = category.ParentId == null ? category.IsMainSlider : false;
                category.IsMainPage = category.ParentId == null ? category.IsMainPage : false;
                var submitRs = await Service.UpsertCategory(new Core.Helpers.AuditWrapDto<CategoryUpsertDto>(){UserId = Security.User.Id, Dto = category});
                if(submitRs.Ok){
                    DialogService.Close(category);
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            } catch(Exception ex){
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        private async Task<string> PrepareUniqueImageName(IBrowserFile item){
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }
        private async Task DirectoryControl(){
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "CategoryImages");
            if(!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
          protected async Task LoadFiles(InputFileChangeEventArgs e,bool mobile){
            IsShowLoadingFile = true;
            foreach(var item in e.GetMultipleFiles(maxAllowedFiles)){
                var resized = item;
               
                try{
                    var newFileName = await PrepareUniqueImageName(resized);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "CategoryImages", newFileName);
           

                    category.ImageUrl = newFileName;
                    var itemStream = resized.OpenReadStream(100000000);
                   
                        if (category.ImageUrl.ToLower().Contains("png"))
                        {
                            await FileService.CompressImage(itemStream, "png", path, false,false);
                        }
                        else if (category.ImageUrl.ToLower().Contains("jpg") || category.ImageUrl.ToLower().Contains("jpeg"))
                        {
                            await FileService.CompressImage(itemStream, "jpg", path, false,false);
                        }
                        if (category.ImageUrl.ToLower().Contains("webp"))
                        {
                            await FileService.CompressImage(itemStream, "webp", path, false,false);
                        }
                        else if (category.ImageUrl.ToLower().Contains("gif"))
                        {
                            await FileService.CompressImage(itemStream, "gif", path, false,false);
                        }
                        else
                        {
                            await using FileStream fs = new(path, FileMode.OpenOrCreate);
                            await itemStream.CopyToAsync(fs);
                        }
                    
                   
                    IsShowLoadingFile = false;
                } catch(Exception ex){
                    NotificationService.Notify(NotificationSeverity.Warning,ex.Message+" "+ "Dosya yüklenirken hata oluştu lütfen tekrar deneyiniz.");
                }
            }
        }
    }
}
