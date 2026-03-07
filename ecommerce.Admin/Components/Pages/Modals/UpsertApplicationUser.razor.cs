using AutoMapper;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

using ecommerce.Admin.Domain.Dtos.UserMenuDto;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
namespace ecommerce.Admin.Components.Pages.Modals;

public partial class UpsertApplicationUser
{
    [Inject] private DialogService DialogService { get; set; }
    [Inject] private NotificationService NotificationService { get; set; }
    [Inject] private IMapper Mapper { get; set; }
    [Inject] private IIdentityUserService IdentityUserService { get; set; }
    [Inject] private IValidator<IdentityUserUpsertDto> IdentityUserValidator { get; set; }
    [Inject] private IMenuService MenuService { get; set; }
    [Inject] private ITenantProvider TenantProvider { get; set; }

    [Inject] private ISalesPersonService SalesPersonService { get; set; }
    [Inject] private IUserBranchService UserBranchService { get; set; }
    [Inject] private ICorporationService CorporationService { get; set; }
    [Inject] private IBranchService BranchService { get; set; }
    [Inject] private ecommerce.Admin.Domain.Interfaces.IPcPosService PcPosService { get; set; }
    
    [Parameter] public int? Id { get; set; }
    
    private IdentityUserUpsertDto EditingEntity { get; set; } = new();
    private RadzenFluentValidator<IdentityUserUpsertDto> IdentityUserValidatorRef { get; set; }
    private List<IdentityRoleListDto> Roles { get; set; } = new();
    private List<Menu> AllMenus { get; set; } = new();
    private List<SalesPersonListDto> SalesPersons { get; set; } = new();
    private List<ecommerce.Admin.Domain.Dtos.HierarchicalDto.CorporationListDto> Corporations { get; set; } = new();
    private List<ecommerce.Admin.Domain.Dtos.HierarchicalDto.BranchListDto> AllBranches { get; set; } = new();
    private int? SelectedCorporationId { get; set; }
    private List<ecommerce.Admin.Domain.Dtos.HierarchicalDto.BranchListDto> FilteredBranches => 
        SelectedCorporationId.HasValue 
            ? AllBranches.Where(b => EditingEntity.Branches.All(ub => ub.BranchId != b.Id) && b.CorporationId == SelectedCorporationId.Value).ToList()
            : new List<ecommerce.Admin.Domain.Dtos.HierarchicalDto.BranchListDto>();
    
    // Changed to manage full permission objects

    
    private bool Saving { get; set; }


    protected override async Task OnInitializedAsync()
    {
        var allRoles = (await IdentityUserService.GetRoleListAsync()).Result ?? new List<IdentityRoleListDto>();
        
        if (TenantProvider.IsGlobalAdmin)
        {
            Roles = allRoles;
        }
        else
        {
            // For branch admins, only allow CustomerB2B, B2BADMIN, Accountant and Plasiyer roles
            Roles = allRoles.Where(r => r.Name == "CustomerB2B" || r.Name.ToUpper() == "B2BADMIN" || r.Name.ToUpper() == "ACCOUNTANT" || r.Name == "Accountant" || r.Name == "Plasiyer").ToList();
        }
        
        // Load salespersons
        var salesPersonResponse = await SalesPersonService.GetSalesPersons();
        if (salesPersonResponse.Ok && salesPersonResponse.Result != null)
        {
            SalesPersons = salesPersonResponse.Result;
        }
        
        // Load all menus for the tree
        var menuResponse = await MenuService.GetMenuHierarchy();
        if (menuResponse.Ok)
        {
            AllMenus = menuResponse.Result ?? new List<Menu>();
        }

        // Load corporations and branches
        var corpResponse = await CorporationService.GetAllActiveCorporations();
        if (corpResponse.Ok)
        {
            Corporations = corpResponse.Result;
        }

        // Load all branches (we'll filter by corporation in the UI)
        var branchResponse = await BranchService.GetAllActiveBranches();
        if (branchResponse.Ok)
        {
            AllBranches = branchResponse.Result;
        }

        // Load PcPos definitions for CaseIds (kullanıcı ataması için - CanView kontrolü yok)
        var pcPosResponse = await PcPosService.GetPcPosForUserAssignment();
        if (pcPosResponse.Ok && pcPosResponse.Result != null)
        {
            PcPosDefinitions = pcPosResponse.Result;
        }
        
        if (Id.HasValue)
        {
            var response = await IdentityUserService.GetAsync(Id.Value);
            if (!response.Ok)
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                DialogService.Close();
                return;
            }
            EditingEntity = response.Result;

            // Parse CaseIds to SelectedPcPosIds (PcPos tanım ID'leri, virgülle ayrılmış)
            if (!string.IsNullOrWhiteSpace(EditingEntity.CaseIds))
            {
                SelectedPcPosIds = EditingEntity.CaseIds
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => int.TryParse(s, out _))
                    .Select(int.Parse)
                    .ToList();
            }

            // Load user's branch assignments
            var userBranchesResponse = await UserBranchService.GetUserBranches(Id.Value);
            if (userBranchesResponse.Ok && userBranchesResponse.Result != null)
            {
                EditingEntity.Branches = userBranchesResponse.Result.Select(ub => new ecommerce.Admin.Domain.Dtos.HierarchicalDto.UserBranchUpsertDto
                {
                    Id = ub.Id,
                    UserId = ub.UserId,
                    BranchId = ub.BranchId,
                    IsDefault = ub.IsDefault
                }).ToList();
            }
        }
        
        await InvokeAsync(StateHasChanged);
    }

    private async Task FormSubmit()
    {
        Saving = true;
        
        // PcPos: Seçilen PcPos tanımlarını CaseIds'e yaz, UserType=2 otomatik
        if (EditingEntity.IsPcPosUser)
        {
            EditingEntity.UserType = 2; // POS kullanıcısı
            EditingEntity.CaseIds = SelectedPcPosIds != null && SelectedPcPosIds.Any()
                ? string.Join(",", SelectedPcPosIds)
                : null;
        }
        else
        {
            EditingEntity.CaseIds = null;
            EditingEntity.CompanyCode = null;
            EditingEntity.IsEdit = false;
            EditingEntity.UserType = null;
        }
        
        // Check if new user before upsert
        bool isNewUser = !EditingEntity.Id.HasValue || EditingEntity.Id.Value == 0;

        var response = await IdentityUserService.UpsertAsync(EditingEntity);
        if (response.Ok)
        {
            // Auto-assign current branch for B2B Admins creating new users
            if (isNewUser && !TenantProvider.IsGlobalAdmin)
            {
                var currentBranchId = TenantProvider.GetCurrentBranchId();
                if (currentBranchId > 0)
                {
                    var userId = response.Result;
                    var branchDto = new List<ecommerce.Admin.Domain.Dtos.HierarchicalDto.UserBranchUpsertDto>
                    {
                        new() {
                            BranchId = currentBranchId,
                            UserId = userId,
                            IsDefault = true
                        }
                    };
                    
                    await UserBranchService.UpsertUserBranches(userId, branchDto);
                }
            }

            NotificationService.Notify(NotificationSeverity.Success, "Kullanıcı başarıyla kaydedildi");
            
            DialogService.Close(EditingEntity);
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        Saving = false;
    }





    private Menu? FindMenuById(int menuId, List<Menu> menus)
    {
        foreach (var menu in menus)
        {
            if (menu.Id == menuId)
                return menu;
                
            if (menu.InverseParent != null && menu.InverseParent.Count > 0)
            {
                var found = FindMenuById(menuId, menu.InverseParent.ToList());
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    private int? SelectedBranchId { get; set; }
    private bool SavingBranches { get; set; }
    private List<ecommerce.Admin.Domain.Dtos.PcPosDto.PcPosListDto> PcPosDefinitions { get; set; } = new();
    private List<int> SelectedPcPosIds { get; set; } = new();

    private void AddBranch()
    {
        if (!SelectedBranchId.HasValue || SelectedBranchId.Value == 0)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Lütfen bir şube seçiniz");
            return;
        }

        // Check if already added
        if (EditingEntity.Branches.Any(b => b.BranchId == SelectedBranchId.Value))
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Bu şube zaten eklenmiş");
            return;
        }

        // Add the branch
        EditingEntity.Branches.Add(new ecommerce.Admin.Domain.Dtos.HierarchicalDto.UserBranchUpsertDto
        {
            BranchId = SelectedBranchId.Value,
            UserId = EditingEntity.Id ?? 0,
            IsDefault = EditingEntity.Branches.Count == 0 // First one is default
        });

        SelectedBranchId = null;
        SelectedCorporationId = null;
        StateHasChanged();
    }

    private void RemoveBranch(int branchId)
    {
        var branch = EditingEntity.Branches.FirstOrDefault(b => b.BranchId == branchId);
        if (branch != null)
        {
            EditingEntity.Branches.Remove(branch);

            // If removed branch was default, make the first one default
            if (branch.IsDefault && EditingEntity.Branches.Count > 0)
            {
                EditingEntity.Branches.First().IsDefault = true;
            }

            StateHasChanged();
        }
    }

    private void SetDefaultBranch(int branchId)
    {
        foreach (var branch in EditingEntity.Branches)
        {
            branch.IsDefault = branch.BranchId == branchId;
        }
        StateHasChanged();
    }

    private async Task SaveBranchAssignments()
    {
        if (!EditingEntity.Id.HasValue)
        {
            NotificationService.Notify(NotificationSeverity.Warning, "Önce kullanıcıyı kaydedin");
            return;
        }

        SavingBranches = true;

        var response = await UserBranchService.UpsertUserBranches(EditingEntity.Id.Value, EditingEntity.Branches);
        if (response.Ok)
        {
            NotificationService.Notify(NotificationSeverity.Success, "Şube atamaları kaydedildi");
        }
        else
        {
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }

        SavingBranches = false;
        await InvokeAsync(StateHasChanged);
    }

    private string GetBranchDisplayName(int branchId)
    {
        var branch = AllBranches.FirstOrDefault(b => b.Id == branchId);
        if (branch == null) return "Bilinmeyen Şube";

        var corp = Corporations.FirstOrDefault(c => c.Id == branch.CorporationId);
        return corp != null ? $"{corp.Name} - {branch.Name}" : branch.Name;
    }


    private async Task ShowErrors()
    {
        await DialogService.OpenAsync<ValidationModal>("Uyari", new Dictionary<string, object>
        {
            { "Errors", IdentityUserValidatorRef.GetValidationMessages().Select(p => new Dictionary<string, string> { { p.Key, p.Value } }).ToList() }
        });
    }

    private void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close();
    }
}
