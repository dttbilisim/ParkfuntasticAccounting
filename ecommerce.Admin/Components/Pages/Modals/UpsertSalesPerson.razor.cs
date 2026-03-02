using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Domain.Dtos.RegionDto;
using ecommerce.Admin.Domain.Dtos.MonthDto;
using ecommerce.Admin.Domain.Dtos.CustomerWorkPlanDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
using Microsoft.AspNetCore.Components.Web;
using System.Linq;
using System;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class UpsertSalesPerson
    {
        #region Injection
        [Inject] protected IJSRuntime JSRuntime { get; set; }
        [Inject] protected NavigationManager NavigationManager { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected TooltipService TooltipService { get; set; }
        [Inject] protected ContextMenuService ContextMenuService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }
        [Inject] public ISalesPersonService SalesPersonService { get; set; }
        [Inject] public ICityService CityService { get; set; }
        [Inject] public ITownService TownService { get; set; }
        [Inject] public IRegionService RegionService { get; set; }
        [Inject] public ICorporationService CorporationService { get; set; }
        [Inject] public IBranchService BranchService { get; set; }
        [Inject] public IMapper Mapper { get; set; }
        [Inject] protected AuthenticationService Security { get; set; }
        [Inject] public ecommerce.Core.Interfaces.ITenantProvider TenantProvider { get; set; }
        #endregion

        [Parameter] public int? Id { get; set; }

        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected SalesPersonUpsertDto salesPerson = new();
        public bool SmsPermission { get; set; } = true;
        protected IEnumerable<CityListDto> Cities { get; set; } = new List<CityListDto>();
        protected IEnumerable<TownListDto> Towns { get; set; } = new List<TownListDto>();
        protected IEnumerable<RegionListDto> Regions { get; set; } = new List<RegionListDto>();
        protected IEnumerable<CorporationListDto> Corporations { get; set; } = new List<CorporationListDto>();
        protected IEnumerable<BranchListDto> Branches { get; set; } = new List<BranchListDto>();
        protected IEnumerable<CustomerListDto> AssignedCustomers { get; set; } = new List<CustomerListDto>();
        protected int? SelectedCityId { get; set; }
        protected int? SelectedTownId { get; set; }
        protected int? SelectedRegionId { get; set; }
        
        // Work Plan properties
        protected int? SelectedWorkPlanCustomerId { get; set; }
        protected int? SelectedWorkPlanDayOfWeek { get; set; }
        protected int? SelectedWorkPlanMonthId { get; set; }
        protected IEnumerable<MonthListDto> Months { get; set; } = new List<MonthListDto>();

        protected IEnumerable<CustomerWorkPlanListDto> WorkPlans { get; set; } = new List<CustomerWorkPlanListDto>();

        // Branch Management
        protected int? SelectedBranchCorporationId { get; set; }
        protected int? SelectedBranchId { get; set; }
        protected bool SelectedBranchIsDefault { get; set; }

        protected IEnumerable<BranchListDto> FilteredBranches { get; set; } = new List<BranchListDto>();
        protected RadzenDataGrid<SalesPersonBranchUpsertDto>? branchesGrid;

        protected override async Task OnInitializedAsync()
        {
            await LoadCities();
            await LoadRegions();
            await LoadMonths();
            await LoadCorporations();

            if (Id.HasValue)
            {
                var response = await SalesPersonService.GetSalesPersonById(Id.Value);
                if (response.Ok)
                {
                    salesPerson = response.Result;
                    SmsPermission = salesPerson.SmsPermission;
                    
                    if (salesPerson.CityId.HasValue)
                    {
                        await LoadTowns(salesPerson.CityId.Value);
                    }

                    if (salesPerson.Status == (int)EntityStatus.Deleted)
                        IsSaveButtonDisabled = true;

                    await LoadAssignedCustomers();
                    await LoadWorkPlans();
                    if (branchesGrid != null) await branchesGrid.Reload();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            else
            {
                // Pre-select current corporation/branch if not global admin
                if (!TenantProvider.IsGlobalAdmin)
                {
                    SelectedBranchCorporationId = TenantProvider.GetCurrentCorporationId();
                    if (SelectedBranchCorporationId > 0)
                    {
                        await LoadBranches(SelectedBranchCorporationId.Value);
                        FilteredBranches = Branches;
                        SelectedBranchId = TenantProvider.GetCurrentBranchId();
                        SelectedBranchIsDefault = true;
                    }
                }
            }
        }
        
        private async Task LoadCities()
        {
            var result = await CityService.GetCities();
            if (result.Ok) Cities = result.Result ?? new List<CityListDto>();
        }

        private async Task LoadRegions()
        {
            var result = await RegionService.GetRegions();
            if (result.Ok) Regions = result.Result ?? new List<RegionListDto>();
        }

        private async Task LoadMonths()
        {
            var result = await SalesPersonService.GetMonths();
            if (result.Ok) Months = result.Result ?? new List<MonthListDto>();
        }

        private async Task LoadCorporations()
        {
            var result = await CorporationService.GetAllActiveCorporations();
            if (result.Ok) Corporations = result.Result ?? new List<CorporationListDto>();
        }

        private async Task LoadWorkPlans()
        {
            if (!Id.HasValue) return;
            var result = await SalesPersonService.GetWorkPlansBySalesPerson(Id.Value);
            if (result.Ok)
            {
                WorkPlans = result.Result ?? new List<CustomerWorkPlanListDto>();
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task LoadTowns(int cityId)
        {
            var result = await TownService.GetTownsByCityId(cityId);
            if (result.Ok) Towns = result.Result ?? new List<TownListDto>();
            else Towns = new List<TownListDto>();
        }

        private async Task LoadBranches(int corporationId)
        {
            var result = await BranchService.GetBranchesByCorporationId(corporationId);
            if (result.Ok) Branches = result.Result ?? new List<BranchListDto>();
            else Branches = new List<BranchListDto>();
        }


        private async Task OnCityChange(object value)
        {
            salesPerson.TownId = null;
            if (value is int cityId)
            {
                await LoadTowns(cityId);
            }
            else
            {
                Towns = new List<TownListDto>();
            }
        }

        protected async Task FormSubmit(SalesPersonUpsertDto args)
        {
            try
            {
                args.Id = Id;
                if (!args.Id.HasValue)
                {
                    args.Status = (int)EntityStatus.Active;
                    args.StatusBool = true;
                }
                else
                {
                    args.StatusBool = args.Status != (int)EntityStatus.Passive && args.Status != (int)EntityStatus.Deleted;
                }
                args.SmsPermission = SmsPermission;
                // salesPerson.City and Town logic removed as we use IDs now

                var submitRs = await SalesPersonService.UpsertSalesPerson(new Core.Helpers.AuditWrapDto<SalesPersonUpsertDto>()
                {
                    UserId = Security.User.Id,
                    Dto = args
                });
                if (submitRs.Ok)
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Başarılı",
                        Detail = "Plasiyer kaydedildi."
                    });
                    DialogService.Close(args);
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

        private async Task LoadAssignedCustomers()
        {
            if (!Id.HasValue) return;
            var response = await SalesPersonService.GetCustomersOfSalesPerson(Id.Value);
            if (response.Ok && response.Result != null)
            {
                AssignedCustomers = response.Result;
                await InvokeAsync(StateHasChanged);
            }
        }

        protected async Task AssignRegionCustomers()
        {
            try
            {
                if (!Id.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Önce plasiyeri kaydedin");
                    return;
                }

                if (!SelectedRegionId.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Bölge seçiniz");
                    return;
                }

                var response = await SalesPersonService.AssignCustomersToSalesPerson(Id.Value, SelectedRegionId.Value);
                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Bölgedeki cari(ler) plasiyere bağlandı.");
                    await LoadAssignedCustomers();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected List<object> GetDayOfWeekOptions()
        {
            return new List<object>
            {
                new { Text = "Pazartesi", Value = 1 },
                new { Text = "Salı", Value = 2 },
                new { Text = "Çarşamba", Value = 3 },
                new { Text = "Perşembe", Value = 4 },
                new { Text = "Cuma", Value = 5 },
                new { Text = "Cumartesi", Value = 6 },
                new { Text = "Pazar", Value = 0 }
            };
        }

        protected async Task AddWorkPlan()
        {
            try
            {
                if (!Id.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Önce plasiyeri kaydedin");
                    return;
                }

                if (!SelectedWorkPlanCustomerId.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Lütfen cari seçiniz");
                    return;
                }

                if (!SelectedWorkPlanDayOfWeek.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Lütfen gün seçiniz");
                    return;
                }

                if (!SelectedWorkPlanMonthId.HasValue)
                {
                    NotificationService.Notify(NotificationSeverity.Warning, "Lütfen ay seçiniz");
                    return;
                }

                var workPlan = new CustomerWorkPlanUpsertDto
                {
                    SalesPersonId = Id.Value,
                    CustomerId = SelectedWorkPlanCustomerId.Value,
                    DayOfWeek = SelectedWorkPlanDayOfWeek.Value,
                    MonthId = SelectedWorkPlanMonthId.Value
                };

                var response = await SalesPersonService.UpsertWorkPlan(new Core.Helpers.AuditWrapDto<CustomerWorkPlanUpsertDto>
                {
                    UserId = Security.User.Id,
                    Dto = workPlan
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Çalışma planı eklendi");
                    SelectedWorkPlanCustomerId = null;
                    SelectedWorkPlanDayOfWeek = null;
                    SelectedWorkPlanMonthId = null;
                    await LoadWorkPlans();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected async Task DeleteWorkPlan(int workPlanId)
        {
            try
            {
                var response = await SalesPersonService.DeleteWorkPlan(new Core.Helpers.AuditWrapDto<CustomerWorkPlanDeleteDto>
                {
                    UserId = Security.User.Id,
                    Dto = new CustomerWorkPlanDeleteDto { Id = workPlanId }
                });

                if (response.Ok)
                {
                    NotificationService.Notify(NotificationSeverity.Success, "Çalışma planı silindi");
                    await LoadWorkPlans();
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(NotificationSeverity.Error, $"Hata: {ex.Message}");
            }
        }

        protected async Task OnBranchCorporationChange(object value)
        {
            SelectedBranchId = null;
            if (value is int corpId)
            {
               var result = await BranchService.GetBranchesByCorporationId(corpId);
               if (result.Ok) FilteredBranches = result.Result ?? new List<BranchListDto>();
               else FilteredBranches = new List<BranchListDto>();
            }
            else
            {
                FilteredBranches = new List<BranchListDto>();
            }
        }

        protected void AddBranch()
        {
             if (!SelectedBranchId.HasValue || !SelectedBranchCorporationId.HasValue) 
             {
                 NotificationService.Notify(NotificationSeverity.Warning, "Şirket ve Şube seçiniz");
                 return;
             }

             if (salesPerson.Branches.Any(b => b.BranchId == SelectedBranchId.Value))
             {
                 NotificationService.Notify(NotificationSeverity.Warning, "Bu şube zaten ekli");
                 return;
             }
             
             // Check if trying to add multiple defaults (optional, basic logic)
             if (SelectedBranchIsDefault && salesPerson.Branches.Any(b => b.IsDefault))
             {
                  // Maybe uncheck others? Or forbid?
                  // Let's just warn or allow (backend handles multiple defaults by resetting others? UserBranchService did that. SalesPersonService didn't implement that yet but can be added or handled by UI).
                  // For now, let's just uncheck others in list
                  foreach (var b in salesPerson.Branches) b.IsDefault = false;
             }

             var branchDto = FilteredBranches.FirstOrDefault(b => b.Id == SelectedBranchId.Value);
             var corpDto = Corporations.FirstOrDefault(c => c.Id == SelectedBranchCorporationId.Value);

             salesPerson.Branches.Add(new SalesPersonBranchUpsertDto
             {
                 BranchId = SelectedBranchId.Value,
                 BranchName = branchDto?.Name,
                 CorporationId = SelectedBranchCorporationId.Value,
                 CorporationName = corpDto?.Name,
                 IsDefault = SelectedBranchIsDefault
             });

             if (SelectedBranchIsDefault)
             {
                 salesPerson.BranchId = SelectedBranchId.Value;
                 // Reset other defaults in the UI list for consistency
                 foreach (var b in salesPerson.Branches.Where(x => x.BranchId != SelectedBranchId.Value)) b.IsDefault = false;
             }
             
             if(branchesGrid != null) branchesGrid.Reload();
             SelectedBranchId = null;
             SelectedBranchIsDefault = false;
        }

        protected void DeleteBranch(SalesPersonBranchUpsertDto branch)
        {
             salesPerson.Branches.Remove(branch);
             if (salesPerson.BranchId == branch.BranchId)
             {
                 salesPerson.BranchId = salesPerson.Branches.FirstOrDefault(b => b.IsDefault)?.BranchId 
                                        ?? salesPerson.Branches.FirstOrDefault()?.BranchId;
             }
             if(branchesGrid != null) branchesGrid.Reload();
        }
    }
}

