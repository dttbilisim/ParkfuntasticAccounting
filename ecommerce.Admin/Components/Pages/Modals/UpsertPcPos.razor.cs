using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PcPosDto;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Dtos;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Microsoft.AspNetCore.Components.Web;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertPcPos
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] public IPcPosService PcPosService { get; set; }
        [Inject] public IMapper Mapper { get; set; }
        [Inject] public ICorporationService CorporationService { get; set; }
        [Inject] public IBranchService BranchService { get; set; }
        [Inject] public IWarehouseService WarehouseService { get; set; }
        [Inject] public IPaymentTypeService PaymentTypeService { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        #endregion

        [Parameter] public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected PcPosUpsertDto pcpos = new();
        public bool Status { get; set; } = true;

        protected IEnumerable<CorporationListDto> corporations;
        protected IEnumerable<BranchListDto> branches;
        protected IEnumerable<WarehouseListDto> warehouses;
        protected IEnumerable<SelectItemDto<int?>> PaymentTypeOptions = new List<SelectItemDto<int?>>();

        protected override async Task OnInitializedAsync()
        {
            await LoadCorporations();
            
            var paymentTypeRs = await PaymentTypeService.GetAllPaymentTypes();
            if (paymentTypeRs.Ok && paymentTypeRs.Result != null)
            {
                PaymentTypeOptions = paymentTypeRs.Result
                    .Select(pt => new SelectItemDto<int?> { Text = pt.Name, Value = pt.Id })
                    .ToList();
            }
            
            if (Id.HasValue)
            {
                var response = await PcPosService.GetPcPosById(Id.Value);
                if (response.Ok)
                {
                    pcpos = response.Result;
                    Status = pcpos.Status == (int)EntityStatus.Passive || pcpos.Status == (int)EntityStatus.Deleted ? false : true;

                    if (pcpos.Status == EntityStatus.Deleted.GetHashCode())
                        IsSaveButtonDisabled = true;

                    // Load dependent data based on loaded entity
                    if (pcpos.CorporationId > 0) await LoadBranches(pcpos.CorporationId);
                    if (pcpos.BranchId > 0) await LoadWarehouses(pcpos.BranchId);
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
        }

        protected async Task LoadCorporations()
        {
            var result = await CorporationService.GetAllActiveCorporations();
            if (result.Ok)
            {
                corporations = result.Result;
            }
        }

        protected async Task LoadBranches(int corporationId)
        {
            var result = await BranchService.GetBranchesByCorporationId(corporationId);
            if (result.Ok)
            {
                branches = result.Result;
            }
        }

        protected async Task LoadWarehouses(int branchId)
        {
            var result = await WarehouseService.GetAllWarehouses();
            if (result.Ok)
            {
                 // Filter by BranchId
                warehouses = result.Result.Where(x => x.BranchId == branchId).ToList();
            }
        }

        protected async Task OnCorporationChange(object value)
        {
            if (value is int corpId)
            {
                pcpos.CorporationId = corpId;
                pcpos.BranchId = 0; // Reset Branch
                pcpos.WarehouseId = 0; // Reset Warehouse
                await LoadBranches(corpId);
                branches = branches ?? new List<BranchListDto>();
                warehouses = new List<WarehouseListDto>(); // Clear warehouses
            }
        }

        protected async Task OnBranchChange(object value)
        {
             if (value is int branchId)
            {
                pcpos.BranchId = branchId;
                pcpos.WarehouseId = 0; // Reset Warehouse
                await LoadWarehouses(branchId);
                warehouses = warehouses ?? new List<WarehouseListDto>();
            }
        }

        protected async Task FormSubmit()
        {
            try
            {
                pcpos.Id = Id;
                pcpos.StatusBool = Status;

                var submitRs = await PcPosService.UpsertPcPos(new Core.Helpers.AuditWrapDto<PcPosUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = pcpos
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "PcPos kaydedildi."
                    });
                    DialogService.Close(pcpos);
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

