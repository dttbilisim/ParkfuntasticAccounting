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
    public partial class Corporations
    {
        [Inject] public AuthenticationService AuthenticationService { get; set; } = null!;
        
        RadzenDataGrid<CorporationListDto> grid = null!;
        IEnumerable<CorporationListDto> corporations = new List<CorporationListDto>();

        protected override async Task OnInitializedAsync()
        {
            await LoadData();
        }

        async Task LoadData()
        {
            var result = await CorporationService.GetCorporations(new PageSetting { Take = 100, Skip = 0 });
            if (result.Ok)
            {
                corporations = result.Result.Data.ToList();
            }
        }

        async Task OpenUpsertModal(int? id = null)
        {
            var result = await DialogService.OpenAsync<Modals.UpsertCorporation>(id == null ? "Yeni Şirket" : "Şirket Düzenle",
                new Dictionary<string, object> { { "Id", id } },
                new DialogOptions { Width = "700px" });

            if (result == true)
            {
                await LoadData();
                await grid.Reload();
            }
        }

        async Task DeleteCorporation(int id)
        {
            var confirm = await DialogService.Confirm("Bu şirketi silmek istediğinize emin misiniz?", "Silme Onayı", new ConfirmOptions { OkButtonText = "Evet", CancelButtonText = "Hayır" });
            if (confirm == true)
            {
                var user = AuthenticationService.User;
                var result = await CorporationService.DeleteCorporation(id, user.Id);
                if (result.Ok)
                {
                    NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Success, Summary = "Başarılı", Detail = "Şirket silindi" });
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
