using ecommerce.Admin.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Shared
{
    public class AdminDataGrid<TItem> : RadzenDataGrid<TItem>
    {
        [Inject]
        public IStringLocalizer<Culture_TR> Loc { get; set; } = default!;

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            // Directly assigning Turkish values for reliability
            // If we want to use Loc, we should ensure the ResX works, but this guarantees the user request is met.
            
            ContainsText = "İçeriyorsa";
            DoesNotContainText = "İçermiyorsa";
            StartsWithText = "İle başlayan";
            EndsWithText = "Metinle biten";
            EqualsText = "Eşittir";
            NotEqualsText = "Eşit değildir";
            LessThanText = "Küçüktür";
            LessThanOrEqualsText = "Küçüktür veya Eşittir";
            GreaterThanText = "Büyüktür";
            GreaterThanOrEqualsText = "Büyüktür veya Eşittir";
            IsNullText = "Boş";
            IsNotNullText = "Boş olmayan";
            ApplyFilterText = "Tamam";
            ClearFilterText = "Temizle";
            FilterText = "Filtrele";
            AndOperatorText = "Ve";
            OrOperatorText = "Veya";

            EmptyText = "Veri bulunamadı.";
            GroupPanelText = "Gruplamak için bir sütun başlığını buraya sürükleyip bırakın.";
        }
    }
}
