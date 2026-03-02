using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Hierarchical
{
    public partial class Branches
    {
        [Inject] public AuthenticationService AuthenticationService { get; set; } = null!;
        
        RadzenDataGrid<BranchListDto> grid = null!;
        IEnumerable<BranchListDto> branches = new List<BranchListDto>();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        async Task LoadData()
        {
            var result = await BranchService.GetBranches(new PageSetting { Take = 100, Skip = 0 });
            if (result.Ok)
            {
                branches = result.Result.Data.ToList();
            }
        }

        async Task OpenUpsertModal(int? id = null)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertBranch>(null,
                new Dictionary<string, object> { { "Id", id } },
                new DialogOptions 
                { 
                    Width = "700px", 
                    Height = "auto", 
                    Style = "max-height: 90vh;", 
                    ShowTitle = false, 
                    ShowClose = false,
                    Resizable = true, 
                    Draggable = true 
                });

            if (result == true)
            {
                await LoadData();
                await grid.Reload();
            }
        }

        async Task DeleteBranch(int id)
        {
            var confirm = await DialogService.Confirm("Bu şubeyi silmek istediğinize emin misiniz?", "Silme Onayı", new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm == true)
            {
                var user = AuthenticationService.User;
                var result = await BranchService.DeleteBranch(id, user.Id);
                if (result.Ok)
                {
                    NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "Şube silindi" });
                    await LoadData();
                    await grid.Reload();
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Error, Summary = "Hata", Detail = result.Metadata?.Message });
                }
            }
        }
    }
}
