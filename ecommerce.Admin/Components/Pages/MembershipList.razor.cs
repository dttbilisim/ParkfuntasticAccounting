using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Emailing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages{
    public partial class MembershipList{
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] protected IMembershipService _membershipService{get;set;}
        [Inject] protected UserManager<ApplicationUser> _userManager{get;set;}
        [Inject] protected IEmailService _emailService{get;set;}
        int count;
        protected List<MembershipListDto> memberships = null;
        protected RadzenDataGrid<MembershipListDto> ? radzenDataGrid = new();
        private PageSetting pager;
        protected async Task AddButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertMembership>("Üye Ekle/Düzenle", null);
            await radzenDataGrid.Reload();
        }
        protected async Task EditRow(MembershipListDto args){
            await DialogService.OpenAsync<UpsertMembership>("Üye Düzenle", new Dictionary<string, object>{{"Id", args.Id}});
            await radzenDataGrid.Reload();
        }
        protected async Task GridDeleteButtonClick(MouseEventArgs args, MembershipListDto membership){
            if(await DialogService.Confirm("Seçilen yeni üyeyi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await _membershipService.DeleteMembership(new Core.Helpers.AuditWrapDto<MembershipDeleteDto>(){Dto = new MembershipDeleteDto(){Id = (int) membership.Id}});
                if(deleteResult.Ok){
                    // await InitGridSource();
                    await radzenDataGrid.Reload();
                    StateHasChanged();
                }
            }
        }
        private async Task LoadData(LoadDataArgs args){
            try{
                var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
                args.Filter = args.Filter.Replace("np", "");
                pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
                var response = await _membershipService.GetMembership(pager);
                if(response.Ok){
                    memberships = response.Result.Data?.OrderByDescending(x => x.RegisterDate).ToList();
                    count = response.Result.DataCount;
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }
                StateHasChanged();
            } catch(Exception e){
                Console.WriteLine(e);
            }
        }
        private async Task GridDoneButtonClick(MouseEventArgs args, MembershipListDto membership){
            try{
                if(await DialogService.Confirm("Seçilen yeni üyeyi onaylamak istediğinize emin misiniz?", "Onayla", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var rs = await _membershipService.GetMembershipById((int) membership.Id);
                    var mu = rs.Result;
                    var result = await _membershipService.GetUserToken((int) mu.Id);
                    if(rs.Ok){
                        mu.SendEmail = true;
                        await _membershipService.UpsertMembership(new Core.Helpers.AuditWrapDto<MembershipUpsertDto>(){Dto = mu, UserId = 1});
                        await _emailService.SendNewUserTokenEmail((mu.FirstName + " " + mu.LastName), mu.EmailAddress, result.Result.Token);
                        NotificationService.Notify(NotificationSeverity.Success, "Yeni üye onaylandı ve onay emaili gönderildi.");
                    }
                    await radzenDataGrid.Reload();
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Hata oluştu"});
            }
        }
    }
}
