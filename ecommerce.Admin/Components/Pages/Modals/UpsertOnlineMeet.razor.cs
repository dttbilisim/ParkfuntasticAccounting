using AutoMapper;
using ecommerce.Admin.Domain.Concreate;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Admin.Domain.Dtos.Scheduler;
using ecommerce.Admin.Domain.Dtos.ZoomDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertOnlineMeet{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected IOnlineMeetService _OnlineMeetService{get;set;}
    [Inject] protected IZoomService _zoomService{get;set;}
    [Inject] public ICompanyService _companyService{get;set;}
    [Inject] public IMapper Mapper{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] public IConfiguration Configuration{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    protected RadzenDataGrid<OnlineMeetDto> ? radzenDataGrid = new();
    private RadzenDataGrid<OnlineMeetCalendarPharmacy> RadzenDataGrid{get;set;}
    public OnlineMeetUpsertDto _OnlineMeetUpsertDto = new();
    private OnlineMeetCalendarPharmacy OnlineMeetCalendarPharmacy = new();
    public List<CompanyListDto> SellerList{get;set;}
    public List<CompanyListDto> CompanyList{get;set;}
    private List<ZoomRecordingFile> _recordingResponses = new();
    public bool Status{get;set;}
    [Parameter] public int Id{get;set;}
    protected bool errorVisible;
    private bool recordShow;
    protected RadzenDataGrid<ZoomRecordingFile> radzenDataGridZoom = new();
    protected RadzenDataGrid<ZoomParticipantResponse> radzenDataGridZoomCompnay = new();
    private List<ZoomParticipantResponse> _participantResponses = new();
    IEnumerable<CompanyWorkingType> CompanyTypes = Enum.GetValues(typeof(CompanyWorkingType)).Cast<CompanyWorkingType>();
    IEnumerable<UserType> UserTypes = Enum.GetValues(typeof(UserType)).Cast<UserType>();

    // protected void ChangeUserType(object value){_EducationCalendar.UserTypeId = (UserType) value;}
    protected override async Task OnInitializedAsync(){
        var sellerList = await _companyService.GetCompanies();
        var companyList = await _companyService.GetCompanies();
        if(sellerList.Ok){
            SellerList = sellerList.Result.Where(x => x.UserType != UserType.Custormer).ToList();
        }
        if(companyList.Ok){
            CompanyList = sellerList.Result.Where(x => x.UserType == UserType.Custormer).ToList();
        }
        
    }
    protected void CancelButtonClick(MouseEventArgs args){}
    protected override async Task OnParametersSetAsync(){
        try{
            if(Id == 0){
                _OnlineMeetUpsertDto.MeetDate = DateTime.Now;
                _OnlineMeetUpsertDto.Duration = 30;
                _OnlineMeetUpsertDto.Password = "1234";
                recordShow = false;
            } else{
                var rs = await _OnlineMeetService.GetMeetById(Id);
                if(rs.Ok){
                    _OnlineMeetUpsertDto = rs.Result;
                    Status = rs.Result.Status == EntityStatus.Active ? true : false;
                  
                    if(_OnlineMeetUpsertDto.MeetDate.AddMinutes(_OnlineMeetUpsertDto.Duration) <= DateTime.Now){
                        var records = await _zoomService.GetMeetingRecordingsAsync(_OnlineMeetUpsertDto.MeetId.Value);
                        if(records != null){
                            _recordingResponses = records.ToList();
                            recordShow=true;
                        }
                        var rscompany = await _zoomService.GetMeetingParticipantsReportAsync(_OnlineMeetUpsertDto.MeetId.Value);
                        if(rscompany != null){
                            _participantResponses = rscompany;
                            StateHasChanged();
                        }
                    }
                  
                   
                }
            }
            StateHasChanged();
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error, e.Message);
        }
    }
    private async Task Submit(){
        try{
            if(_OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Count == 0){
                NotificationService.Notify(NotificationSeverity.Error, "Lütfen Katılımcı Ekleyiniz");
            } else{
                _OnlineMeetUpsertDto.Status = Status ? EntityStatus.Active : EntityStatus.Passive;
                var rs = await _OnlineMeetService.UpsertMeet(new AuditWrapDto<OnlineMeetUpsertDto>{UserId = Security.User.Id, Dto = _OnlineMeetUpsertDto});
                if(rs.Ok){
                    NotificationService.Notify(NotificationSeverity.Success, "Kayıt Edildi");
                    DialogService.Close(null);
                }
            }
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error, e.Message);
        }
    }
    private async Task LoadCompanies(LoadDataArgs args){
        var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
        if(!string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
            var filter = pager.Filter.ToLower();
            pager.Filter = $"FirstName.ToLower().Contains(\"{filter}\") or LastName.ToLower().Contains(\"{filter}\") or GlnNumber.ToLower().Contains(\"{filter}\")";
        }
        if(CompanyList.Count == 0){
            var initialSelectedItems = _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Select(x => x.CompanyId).ToList();
            if(initialSelectedItems.Count > 0){
                pager.Skip = 0;
                pager.Take = initialSelectedItems.Count;
                pager.Filter = $"new int[] {{ {string.Join(", ", initialSelectedItems)} }}.Contains(Id)";
            }
        }
        await InvokeAsync(StateHasChanged);
    }
    private async Task InsertDiscountCompanyCouponRow(){await RadzenDataGrid.InsertRow(OnlineMeetCalendarPharmacy);}
    public async Task AddOrUpdatePharmacy(OnlineMeetCalendarPharmacy newPharmacy){
        var existingPharmacy = _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.FirstOrDefault(p => p.OnlineMeetId == newPharmacy.OnlineMeetId && p.CompanyId == newPharmacy.CompanyId);
        var duplicatePharmacy = _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Any(p => p.CompanyId == newPharmacy.CompanyId);
        if(duplicatePharmacy){
            NotificationService.Notify(NotificationSeverity.Error, "Aynı eczaneden sadece bir katılımcı katılabilir");
            return;
        }
        if(existingPharmacy != null){
            _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Remove(existingPharmacy);
        }
        if(newPharmacy.CompanyId > 0){
            var companyName = CompanyList.FirstOrDefault(x => x.Id == newPharmacy.CompanyId)?.FullName;
            newPharmacy.CompanyName = companyName;
        }
        _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Add(newPharmacy);
        await RadzenDataGrid.UpdateRow(newPharmacy);
        NotificationService.Notify(NotificationSeverity.Success, "Katılımcı Eklendi");
        OnlineMeetCalendarPharmacy = new();
    }
    public async Task RemovePharmacy(OnlineMeetCalendarPharmacy pharmacyToRemove){
        if(pharmacyToRemove != null){
            _OnlineMeetUpsertDto.OnlineMeetCalendarPharmacies.Remove(pharmacyToRemove);
            await RadzenDataGrid.Reload();
        } else{
            Console.WriteLine("Silinecek kayıt bulunamadı.");
        }
    }
    void RowRender(RowRenderEventArgs<OnlineMeetCalendarPharmacy> args){
        if(args.Data.IsApproved){
            args.Attributes.Add("style", $"background-color: #D0F0C0;");
        }
    }
    private void OnCompanyChanged(object value, OnlineMeetCalendarPharmacy context){
        var selectedCompanyId = (int) value;
        var selectedCompany = CompanyList.FirstOrDefault(c => c.Id == selectedCompanyId);
        if(selectedCompany == null) return;
        context.Name = selectedCompany.FirstName;
        context.SurName = selectedCompany.LastName;
        context.Email = selectedCompany.EmailAddress;
    }
    private void OnSellerChanged(object value)
    {
   
        var selectedSellerId = (int)value;
        var selectedSeller = SellerList.FirstOrDefault(s => s.Id == selectedSellerId);
        if(selectedSeller == null) return;
        _OnlineMeetUpsertDto.SellerName = selectedSeller.FullName;
        _OnlineMeetUpsertDto.SellerEmail = selectedSeller.EmailAddress;
        _OnlineMeetUpsertDto.SellerName = selectedSeller.FirstName + " " + selectedSeller.LastName;
    }
}
