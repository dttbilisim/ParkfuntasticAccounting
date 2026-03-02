using ecommerce.Admin.Domain.Dtos.EducationDto;
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
public partial class EducationItemsContent{
    [Inject] protected IJSRuntime JSRuntime{get;set;}
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] public IEducationService _educationService{get;set;}
    [Inject] protected AuthenticationService Security{get;set;}
    private int count;
    protected List<EducationItemsListDto> data = null;
    protected RadzenDataGrid<EducationItemsListDto> ? grid0 = new();
    private PageSetting pager;
    private new DialogOptions DialogOptions = new(){Width = "1200px"};
   
    protected async Task EditRow(EducationItemsListDto args){
        await DialogService.OpenAsync<UpsertEducationItems>("Düzenle", new Dictionary<string, object>{{"Id", args.Id}}, DialogOptions);
         await grid0.Reload();
    }
    protected async Task AddButtonClick(MouseEventArgs args){
         await DialogService.OpenAsync<UpsertEducationItems>("Ekle/Düzenle", null, DialogOptions);
         await grid0.Reload();
    }
    private async Task LoadData(LoadDataArgs args){
        var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
        args.Filter = args.Filter.Replace("np", "");
        pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
        var response = await _educationService.GetEducationItems(pager);
        if(response.Ok && response.Result != null){
            data = response.Result.Data.ToList();
            count = response.Result.DataCount;
        } else{
            NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
        }
        StateHasChanged();
    }
    void RowRender(RowRenderEventArgs<EducationItemsListDto> args){
        if(args.Data.Status == 0) args.Attributes.Add("style", $"background-color: {(args.Data.Status == 0 ? "#FFEFEF" : "White")};");
    }
    protected async Task GridDeleteButtonClick(MouseEventArgs args, EducationItemsListDto input){
        try{
            if(await DialogService.Confirm("Seçilen Kayıt silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await _educationService.DeleteEducationItem(new EducationItemsDeleteDto{Id = (int) input.Id});
                if(deleteResult != null){
                    if(deleteResult != null){
                        if(deleteResult.Ok)
                            await grid0.Reload();
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                    }
                }
            }
        } catch(Exception ex){
            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete Category"});
        }
    }
}
