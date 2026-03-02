using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Admin.Domain.Dtos.Scheduler;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertEducationCalendar{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected IEducationCalendarService _EducationService{get;set;}
    [Inject] public IPharmacyTypeService _pharmacyTypeService{get;set;}
    [Inject] public ICompanyService _companyService{get;set;}
    [Inject] public IMapper Mapper{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    [Inject] public IPharmacyTypeService PharmacyTypeService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    protected RadzenDataGrid<EducationCalendarListDto> ? radzenDataGrid = new();
    public EducationCalendarUpsertDto _EducationCalendar = new();
    protected List<PharmacyTypeListDto> pharmacyTypes = new();
    public bool Status{get;set;}
   
    [Parameter] public int Id{get;set;}
    protected bool errorVisible;
    IEnumerable<CompanyWorkingType> CompanyTypes = Enum.GetValues(typeof(CompanyWorkingType)).Cast<CompanyWorkingType>();
    IEnumerable<UserType> UserTypes = Enum.GetValues(typeof(UserType)).Cast<UserType>();
    protected void ChangeUserType(object value){_EducationCalendar.UserTypeId = (UserType) value;}
    protected override async Task OnInitializedAsync(){
        var pharmacyTypeResponse = await PharmacyTypeService.GetPharmacyTypes();
        if(pharmacyTypeResponse.Ok) pharmacyTypes = pharmacyTypeResponse.Result;
    }
    protected void CancelButtonClick(MouseEventArgs args){}
    protected override async Task OnParametersSetAsync(){
        try{
            if (Id == 0)
            {
                _EducationCalendar.StartDate = DateTime.Now;
                _EducationCalendar.EndDate = DateTime.Now;
            }

            var rs = await _EducationService.GetById(Id);
            if(rs.Ok){
                _EducationCalendar = rs.Result;
                Status = rs.Result.Status==EntityStatus.Active?true:false;
                StateHasChanged();
            }
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error,e.Message );
        }
    }
    private async Task Submit(){
        try{
            _EducationCalendar.Status = Status ? EntityStatus.Active : EntityStatus.Passive;
            var rs = await _EducationService.Upsert(new AuditWrapDto<EducationCalendarUpsertDto>{
                UserId = Security.User.Id,Dto = _EducationCalendar
            });
            if(rs.Ok){
                NotificationService.Notify(NotificationSeverity.Success,"Kayıt Edildi");
                DialogService.Close(null);

            }
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error,e.Message );
        }
    }
}
