using ecommerce.Admin.Domain.Dtos.SearchSynonymDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Models;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
using ecommerce.Core.Helpers;

namespace ecommerce.Admin.Components.Pages
{
    public partial class SearchSynonyms
    {
        #region Injection

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public ISearchSynonymAdminService Service { get; set; }

        [Inject]
        public IAppSettingService AppSettingService { get; set; }

        [Inject]
        public ISearchSynonymService SharedService { get; set; }

        #endregion

        int count;
        protected bool shouldGroupOems = true;
        protected List<SearchSynonymListDto>? synonyms = null;
        protected RadzenDataGrid<SearchSynonymListDto> radzenDataGrid = new();
        private PageSetting? pager;

        protected override async Task OnInitializedAsync()
        {
            await LoadGeneralSettings();
            await base.OnInitializedAsync();
        }

        protected async Task LoadGeneralSettings()
        {
            var metadata = await SharedService.GetSearchMetadataAsync();
            shouldGroupOems = metadata?.ShouldGroupOems ?? true;
        }

        protected async Task OnGroupOemsChange(bool value)
        {
            try
            {
                var settings = new SearchGeneralSettings
                {
                    ShouldGroupOems = value
                };

                await SharedService.SaveGeneralSettingsAsync(settings);

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Başarılı",
                    Detail = "Ayarlar kaydedildi.",
                    Duration = 2000
                });
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Ayarlar kaydedilirken hata oluştu.",
                    Duration = 3000
                });
            }
        }

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertSearchSynonym>("Eş Anlamlı Ekle/Düzenle", null, new DialogOptions { Width = "600px" });
            await radzenDataGrid.Reload();
        }

        protected async Task EditBoostSettingsClick(MouseEventArgs args)
        {
            var response = await AppSettingService.GetValue("Search_BoostWeights");
            if (response.Ok && response.Result != null && response.Result.Id > 0)
            {
                await DialogService.OpenAsync<ManageSearchBoostWeights>("Arama Ağırlıklarını Düzenle", new Dictionary<string, object> {
                    { "Id", response.Result.Id }
                }, new DialogOptions { Width = "800px" });
                // We don't need to reload the grid as this is not in the synonyms table
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Warning, "Arama ağırlık ayarı (Search_BoostWeights) veritabanında bulunamadı.");
            }
        }

        protected async Task EditRow(SearchSynonymListDto args)
        {
            await DialogService.OpenAsync<UpsertSearchSynonym>("Eş Anlamlı Düzenle", new Dictionary<string, object> {
                { "Id", args.Id }
            }, new DialogOptions { Width = "600px" });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, SearchSynonymListDto synonym)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen eş anlamlıyı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteSynonym(new AuditWrapDto<SearchSynonymDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new SearchSynonymDeleteDto { Id = synonym.Id }
                    });

                    if (deleteResult != null)
                    {
                        if (deleteResult.Ok)
                            await radzenDataGrid.Reload();
                        else
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions() { OkButtonText = "Tamam" });
                    }
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = "Silme işlemi başarısız oldu"
                });
            }
        }

        private async Task LoadData(LoadDataArgs args)
        {
            var orderfilter = string.IsNullOrEmpty(args.OrderBy) ? "Id desc" : args.OrderBy.Replace("np", "");
            var filter = string.IsNullOrEmpty(args.Filter) ? "" : args.Filter.Replace("np", "");
            pager = new PageSetting(filter, orderfilter, args.Skip, args.Top);

            var response = await Service.GetSynonyms(pager);
            if (response.Ok && response.Result != null)
            {
                synonyms = response.Result.Data.ToList();
                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }
    }
}
