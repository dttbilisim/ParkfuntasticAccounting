using ecommerce.Admin.Domain.Dtos;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
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
public partial class OnlineMeet{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    [Inject] public INotificationTypeService Service{get;set;}
    [Inject] protected IOnlineMeetService _OnlineMeetService{get;set;}
    [Inject] protected IZoomService _ZoomService{get;set;}
    int count;
    protected List<OnlineMeetDto> _onlineMeeeData = null;
    protected RadzenDataGrid<OnlineMeetDto> ? radzenDataGrid = new();
    private PageSetting pager;
    private async Task LoadData(LoadDataArgs args){
   
        pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
        var response = await _OnlineMeetService.GetOnlineMeet(pager);
        if(response.Ok && response.Result != null){
            _onlineMeeeData = response.Result.Data?.ToList();
            count = response.Result.DataCount;
        } else
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
    }
    protected async Task AddButtonClick(MouseEventArgs args){
        await DialogService.OpenAsync<UpsertOnlineMeet>("Yeni Ekle", null, new DialogOptions(){Width = "1200px", CssClass = "mw-100"});
        await radzenDataGrid.Reload();
    }
    protected async Task EditRow(OnlineMeetDto args){
        await DialogService.OpenAsync<UpsertOnlineMeet>("Düzenle", new Dictionary<string, object>{{"Id", args.Id}}, new DialogOptions(){Width = "1200px", CssClass = "mw-100"});
        await radzenDataGrid.Reload();
    }
    void RowRender(RowRenderEventArgs<OnlineMeetDto> args){
        if(args.Data.MeetDate.AddMinutes(args.Data.Duration)<=DateTime.Now){
            args.Attributes.Add("style", $"background-color: #e6e1e1;");
        } else{
            args.Attributes.Add("style", $"background-color: #b7edc6;");
        }
    }
    protected async Task GridDetailButtonClick(MouseEventArgs args, OnlineMeetDto data){
        try{
           // var zoom = await _ZoomService.GetMeetingRegistrants(data.MeetId.Value);
            await DialogService.OpenAsync<UpsertOnlineMeetDetail>($"Katılımcı Detayı {data.MeetId}", new Dictionary<string, object>{{"Id", data.MeetId}}, new DialogOptions(){Width = "1200px"});
            await radzenDataGrid.Reload();
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete ScaleUnit"});
        }
    }
    protected async Task GridEndMeetingButtonClick(MouseEventArgs args, OnlineMeetDto data){
        try{
            if(await DialogService.Confirm("Toplantı sonlandırılacaktır eminmisiniz?", "Toplantıyı Sonlandır", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                await _ZoomService.EndMeetingAsync(data.MeetId.Value);
                await radzenDataGrid.Reload();
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete ScaleUnit"});
        }
    }
    protected async Task GridDeleteButtonClick(MouseEventArgs args, OnlineMeetDto data){
        try{
            if(await DialogService.Confirm("Seçilen kaydı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await _OnlineMeetService.DeleteMeet(new Core.Helpers.AuditWrapDto<OnlineMeetDeleteDto>(){
                        UserId = Security.User.Id,
                        Dto = new OnlineMeetDeleteDto(){
                            Id = (int) data.Id,
                            MeetId = data.MeetId.Value,
                            SellerId = data.SellerId,
                            SellerName = data.SellerName,
                            Subject = data.Subject,
                            Duration = data.Duration,
                            MeetDate = data.MeetDate,
                            SellerEmail = data.SellerEmail,
                            OnlineMeetCalendarPharmacies = data.OnlineMeetCalendarPharmacies
                        }
                        
                    }
                );
                
                if(deleteResult != null){
                    if(deleteResult.Ok){
                        await _ZoomService.CancelMeetingAsync(data.MeetId.Value);
                        await radzenDataGrid.Reload();
                    } else{
                        await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                    }
                }
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete ScaleUnit"});
        }
    }
}
