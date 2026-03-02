using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Resources;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages
{
    public partial class SurveyResult
    {
        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        private IStringLocalizer<Culture_TR> RadzenLocalizer { get; set; }

        private ISurveyService Service => _service ??= ScopedServices.GetRequiredService<ISurveyService>();
        private ISurveyService? _service;

        [Parameter]
        public int Id { get; set; }

        private Paging<List<SurveyAnswerListDto>> SurveyAnswers { get; set; } = new();

        private RadzenDataGrid<SurveyAnswerListDto> DataGrid { get; set; }

        private List<SurveyAnswerStatisticDto> SurveyAnswerStatistics { get; set; } = new();

        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);

        protected override async Task OnInitializedAsync()
        {
            using (await _loadSemaphore.LockAsync())
            {
                SurveyAnswerStatistics = (await Service.GetSurveyAnswerStatistics(Id)).Result ?? new List<SurveyAnswerStatisticDto>();
            }

            await InvokeAsync(StateHasChanged);
        }

        private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args)
        {
            if (!args.FirstRender)
            {
                return;
            }

            _ = SetRadzenTexts(args.Grid);
        }

        private async Task SetRadzenTexts(RadzenComponent radzenComponent)
        {
            var parameters = ParameterView.FromDictionary(
                RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object?) l.Value)
            );

            await radzenComponent.SetParametersAsync(parameters);

            await InvokeAsync(StateHasChanged);
        }

        private async Task LoadData(LoadDataArgs args)
        {
            using (await _loadSemaphore.LockAsync())
            {
                var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);

                var response = await Service.GetSurveyAnswers(Id, pager);

                if (response.Ok)
                {
                    SurveyAnswers = response.Result;
                }
                else
                {
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                }

                await InvokeAsync(StateHasChanged);
            }
        }
    }
}