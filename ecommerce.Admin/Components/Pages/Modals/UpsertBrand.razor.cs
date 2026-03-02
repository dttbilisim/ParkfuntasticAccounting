using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BrandDto;
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
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Domain.Shared.Dtos.Options;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertBrand
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
        public ICorporationService CorporationService { get; set; }

        [Inject]
        public IBranchService BranchService { get; set; }

        [Inject]
        public IBrandService BrandService { get; set; }

        [Inject]
        public IMapper Mapper { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IFileService FileService { get; set; }

        [Inject]
        public IConfiguration Configuration { get; set; }

        [Inject]
        public ecommerce.Core.Interfaces.ITenantProvider TenantProvider { get; set; }

        [Inject]
        private CdnOptions CdnConfig { get; set; }
        #endregion

        #region Parameters

        [Parameter]
        public int? Id { get; set; }

        #endregion


        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected BrandUpsertDto brand = new();
        public bool Status { get; set; } = true;

        protected List<CorporationListDto> corporations = new();
        protected List<BranchListDto> branches = new();
        protected int? SelectedCorporationId;
        protected bool IsGlobalAdmin => TenantProvider.IsGlobalAdmin;


        protected override async Task OnInitializedAsync()
        {
            if (IsGlobalAdmin)
            {
                var corpRs = await CorporationService.GetAllActiveCorporations();
                if (corpRs.Ok) corporations = corpRs.Result;
            }

            if (Id.HasValue)
            {
                var response = await BrandService.GetBrandById(Id.Value);
                if (response.Ok && response.Result != null)
                {
                    brand = response.Result;
                    Status = brand.Status == (int)EntityStatus.Passive || brand.Status == (int)EntityStatus.Deleted ? false : true;

                    if (brand.BranchId.HasValue)
                    {
                        var allBranchesRs = await BranchService.GetAllActiveBranches();
                        if (allBranchesRs.Ok)
                        {
                            var currentBranch = allBranchesRs.Result.FirstOrDefault(b => b.Id == brand.BranchId.Value);
                            if (currentBranch != null)
                            {
                                SelectedCorporationId = currentBranch.CorporationId;
                                branches = allBranchesRs.Result.Where(b => b.CorporationId == SelectedCorporationId).ToList();
                            }
                        }
                    }

                    if (brand.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;
                }
                else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            else
            {
                // New Brand: Pre-populate context for non-global admins
                if (!IsGlobalAdmin)
                {
                    SelectedCorporationId = TenantProvider.GetCurrentCorporationId();
                    brand.BranchId = TenantProvider.GetCurrentBranchId();
                    
                    // Pre-load branches for the user's corporation just in case they are allowed to pick (B2B Admin)
                    var branchRs = await BranchService.GetAllActiveBranches();
                    if (branchRs.Ok)
                    {
                        branches = branchRs.Result.Where(b => b.CorporationId == SelectedCorporationId).ToList();
                    }
                }
            }
        }

        protected async Task OnCorporationChange()
        {
            brand.BranchId = null;
            if (SelectedCorporationId.HasValue)
            {
                var branchRs = await BranchService.GetAllActiveBranches();
                if (branchRs.Ok)
                {
                    branches = branchRs.Result.Where(b => b.CorporationId == SelectedCorporationId.Value).ToList();
                }
            }
            else
            {
                branches = new();
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                brand.Id = Id;
                brand.StatusBool = Status;
                var submitRs = await BrandService.UpsertBrand(new Core.Helpers.AuditWrapDto<BrandUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = brand
                });
                if (submitRs.Ok)
                {
                    if(submitRs.Metadata.Message.Contains("insert")){
                        NotificationService.Notify(NotificationSeverity.Success, "Kayıt Ekledi");
                    } else
                        if(submitRs.Metadata.Message.Contains("update")){
                            NotificationService.Notify(NotificationSeverity.Success, "Bu marka mevcut güncelleme yapıldı.");
                        }

                    DialogService.Close(brand);
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

        private async Task<string> PrepareUniqueImageName(IBrowserFile item)
        {
            var randomName = Path.GetRandomFileName();
            var extension = Path.GetExtension(item.Name);
            var newFileName = Path.ChangeExtension(randomName, extension);
            return newFileName;
        }

        private async Task DirectoryControl()
        {
            var directoryPath = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BrandImages");
            if (!Directory.Exists(directoryPath)) Directory.CreateDirectory(directoryPath);
        }

        protected async Task LoadFiles(InputFileChangeEventArgs e)
        {
            foreach (var item in e.GetMultipleFiles(5))
            {
                try
                {
                    var newFileName = await PrepareUniqueImageName(item);
                    await DirectoryControl();
                    var path = Path.Combine(Configuration.GetValue<string>("UploadImagePath"), "BrandImages", newFileName);

                    brand.ImageUrl = newFileName;
                    var itemStream = item.OpenReadStream(100000000);

                    if (brand.ImageUrl.ToLower().Contains("png"))
                    {
                        await FileService.CompressImage(itemStream, "png", path, false, false);
                    }
                    else if (brand.ImageUrl.ToLower().Contains("jpg") || brand.ImageUrl.ToLower().Contains("jpeg"))
                    {
                        await FileService.CompressImage(itemStream, "jpg", path, false, false);
                    }
                    else if (brand.ImageUrl.ToLower().Contains("webp"))
                    {
                        await FileService.CompressImage(itemStream, "webp", path, false, false);
                    }
                    else if (brand.ImageUrl.ToLower().Contains("gif"))
                    {
                        await FileService.CompressImage(itemStream, "gif", path, false, false);
                    }
                    else
                    {
                        await using FileStream fs = new(path, FileMode.OpenOrCreate);
                        await itemStream.CopyToAsync(fs);
                    }
                }
                catch (Exception ex)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, ex.Message + " " + "Dosya yüklenirken hata oluştu lütfen tekrar deneyiniz.");
                }
            }
        }
    }
}
