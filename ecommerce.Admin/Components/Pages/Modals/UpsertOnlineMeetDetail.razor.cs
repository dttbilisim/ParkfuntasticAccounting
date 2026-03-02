using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.ZoomDto;
using ecommerce.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertOnlineMeetDetail{
    [Parameter] public long? Id { get; set; }
    [Inject] private IZoomService _zoomService{get;set;}
    protected RadzenDataGrid<ZoomParticipantResponse> radzenDataGridZoom = new();
    private List<ZoomParticipantResponse> _participantResponses = new();
    protected override async Task OnParametersSetAsync(){
        await GetMeetingDetail();
    }
    private async Task GetMeetingDetail(){
        if(Id.HasValue){
            var rs = await _zoomService.GetMeetingParticipantsReportAsync(Id.Value);
            if(rs != null){
                _participantResponses = rs;
                StateHasChanged();
            }
        }
    }


}
