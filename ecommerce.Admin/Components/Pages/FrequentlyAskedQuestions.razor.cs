using ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
using Radzen.Blazor;
namespace ecommerce.Admin.Components.Pages
{
    public partial class FrequentlyAskedQuestions
    {
        #region Injection

        [Inject]
        protected NavigationManager NavigationManager { get; set; }

        [Inject]
        protected DialogService DialogService { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected ContextMenuService ContextMenuService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [Inject]
        protected AuthenticationService Security { get; set; }

        [Inject]
        public IFrequentlyAskedQuestionService Service { get; set; }

        #endregion

        int count;
        protected List<FrequentlyAskedQuestionListDto> sssList = null!;
        protected RadzenDataGrid<FrequentlyAskedQuestionListDto>? radzenDataGrid = new();
        private PageSetting pager;

        #region Load_Events

        private async Task LoadData(LoadDataArgs args)
        {
            var newFilterText = args.Filter;
            //if (dataFilter.Filters.Any())
            //{
            //    //newFilterText = !string.IsNullOrEmpty(newFilterText) ?
            //    //    $"({dataFilter.ToODataFilterString()}) and ({newFilterText})" : dataFilter.ToODataFilterString();
            //}

            pager = new PageSetting(newFilterText, args.OrderBy, args.Skip, args.Top);

            var response = await Service.GetFrequentlyAskedQuestions(pager);
            if (response.Ok)
            {
                if (response.Result.Data is not null)
                    sssList = response.Result.Data.ToList();

                count = response.Result.DataCount;
            }
            else
            {
                NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
            }
            StateHasChanged();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

            if (firstRender)
            {
                //await dataFilter.AddFilter(new CompositeFilterDescriptor()
                //{
                //    Property = "Group",
                //    FilterValue = SSSAndBlogGroup.SSS.GetHashCode(),
                //    FilterOperator = Radzen.FilterOperator.Equals,
                //});
            }
        }

        #endregion

        #region Click_Events

        protected async Task AddButtonClick(MouseEventArgs args)
        {
            await DialogService.OpenAsync<UpsertFrequentlyAskedQuestion>("SSS Ekle", null);
            await radzenDataGrid.Reload();
        }

        protected async Task EditRow(FrequentlyAskedQuestionListDto args)
        {
            await DialogService.OpenAsync<UpsertFrequentlyAskedQuestion>("SSS Düzenle", new Dictionary<string, object> {
                { "Id", args.Id }
            });
            await radzenDataGrid.Reload();
        }

        protected async Task GridDeleteButtonClick(MouseEventArgs args, FrequentlyAskedQuestionListDto brand)
        {
            try
            {
                if (await DialogService.Confirm("Seçilen kayıdı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions()
                {
                    OkButtonText = "Evet",
                    CancelButtonText = "Hayır"
                }) == true)
                {
                    var deleteResult = await Service.DeleteFrequentlyAskedQuestion(new Core.Helpers.AuditWrapDto<FrequentlyAskedQuestionDeleteDto>()
                    {
                        UserId = Security.User.Id,
                        Dto = new FrequentlyAskedQuestionDeleteDto() { Id = brand.Id }
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
                    Summary = $"Error",
                    Detail = $"Unable to delete product"
                });
            }
        }

        #endregion
    }
}
