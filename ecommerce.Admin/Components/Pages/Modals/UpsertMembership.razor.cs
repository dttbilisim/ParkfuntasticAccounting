using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertMembership{
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IMembershipService _membershipService{get;set;}
        [Inject] public IPharmacyTypeService _pharmacyTypeService{get;set;}
        [Inject] public ICompanyService _companyService{get;set;}
        [Inject] public IMapper Mapper{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        public List<CompanyDocumentListDto> CompanyDocumentList{get;set;}
        #region Parameters
        [Parameter] public int ? Id{get;set;}
        #endregion
        protected bool errorVisible;
        protected MembershipUpsertDto membership = new();
        public int MembershipTypeId{get;set;}
        List<CityListDto> cityLists;
        List<TownListDto> townLists;
        public bool IsShowPharmacyTypes = false;
        private List<PharmacyTypeListDto> pharmacyTypes;
        public string BaseUrl{get;set;}
        int CityValue, TownValue;
        IEnumerable<UserType> UserTypes = Enum.GetValues(typeof(UserType)).Cast<UserType>();
        IEnumerable<CompanyWorkingType> CompanyWorkingTypes = Enum.GetValues(typeof(CompanyWorkingType)).Cast<CompanyWorkingType>();
        protected override async Task OnInitializedAsync(){
            BaseUrl = Configuration.GetValue<string>("FileUrl") + "CompanyDocuments";
            if(Id.HasValue){
                var res = await _membershipService.GetCityList();
                var pharmacyTypeResponse = await _pharmacyTypeService.GetPharmacyTypes();
                if(pharmacyTypeResponse.Ok){
                    pharmacyTypes = pharmacyTypeResponse.Result;
                }
                cityLists = res.Result;
                var response = await _membershipService.GetMembershipById(Id.Value);
                if(response.Ok && response.Result != null){
                    membership = response.Result;
                    CityValue = membership.CityId;
                    TownValue = membership.TownId;
                    var result = await _membershipService.GetTownListGetById(CityValue);
                    townLists = result.Result;
                    if(membership.UserType == UserType.Custormer){
                        MembershipTypeId = 1;
                        IsShowPharmacyTypes = true;
                    } else{
                        MembershipTypeId = 2;
                    }
                } else
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                var companyDoucmentList = await _companyService.GetCompanyDocumentList(membership.EmailAddress);
                if(companyDoucmentList.Ok){
                    CompanyDocumentList = companyDoucmentList.Result;
                }
            }
        }
        protected void ChangeUserType(object value){
            if(UserType.Custormer == (UserType) value)
                IsShowPharmacyTypes = true;
            else
                IsShowPharmacyTypes = false;
        }
        protected async Task FormSubmit(){
            try{
                membership.Id = Id;
                membership.CityId = CityValue;
                membership.TownId = TownValue;
                var submitRs = await _membershipService.UpsertMembership(new Core.Helpers.AuditWrapDto<MembershipUpsertDto>(){Dto = membership, UserId = 1});
                if(submitRs.Ok){
                    DialogService.Close(membership);
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            } catch(Exception ex){
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
        private async Task CityIsChangedAsync(object args){
            var result = await _membershipService.GetTownListGetById(CityValue);
            townLists = result.Result;
            //StateHasChanged();
        }
    }
}
