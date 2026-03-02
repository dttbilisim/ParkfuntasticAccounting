using ecommerce.Admin.Domain.Dtos.Scheduler;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;
public partial class EducationCalendarPage{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] public INotificationTypeService Service{get;set;}
    [Inject] protected IEducationCalendarService _educationCalendarService{get;set;}
    int count;
    protected List<EducationCalendarListDto> _educationCalendar = null;
    protected RadzenDataGrid<EducationCalendarListDto> ? radzenDataGrid = new();
    private PageSetting pager;
    private async Task LoadData(LoadDataArgs args){
        pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
        var response = await _educationCalendarService.GetAll(pager);
        if(response.Ok && response.Result != null){
            _educationCalendar = response.Result.Data?.ToList();
            count = response.Result.DataCount;
        } else
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
    }
    protected async Task AddButtonClick(MouseEventArgs args){
         await DialogService.OpenAsync<UpsertEducationCalendar>("Ekle", null);
        await radzenDataGrid.Reload();
    }
    protected async Task EditRow(EducationCalendarListDto args){
        await DialogService.OpenAsync<UpsertEducationCalendar>("Düzenle", new Dictionary<string, object>{{"Id", args.Id}});
        await radzenDataGrid.Reload();
    }
    protected async Task GridDeleteButtonClick(MouseEventArgs args, EducationCalendarListDto data){
        try{
            if(await DialogService.Confirm("Seçilen kaydı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){
                   OkButtonText = "Evet", CancelButtonText = "Hayır"
               }) == true){
                var deleteResult = await _educationCalendarService.Delete(new Core.Helpers.AuditWrapDto<EducationCalendarDeleteDto>(){UserId = Security.User.Id, Dto = new EducationCalendarDeleteDto(){Id = (int) data.Id}});
                if(deleteResult != null){
                    if(deleteResult.Ok)
                        await radzenDataGrid.Reload();
                    else
                        await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                }
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete ScaleUnit"});
        }
    }
}
