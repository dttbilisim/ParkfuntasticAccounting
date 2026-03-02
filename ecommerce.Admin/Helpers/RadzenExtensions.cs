using System.Linq;
using System.Threading.Tasks;
using ecommerce.Admin.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Radzen;

namespace ecommerce.Admin.Helpers
{
    public static class RadzenExtensions
    {
        public static async Task SetTurkishTexts(this RadzenComponent component, IStringLocalizer<Culture_TR> localizer)
        {
            if (component == null || localizer == null) return;
            
            var parameters = ParameterView.FromDictionary(localizer.GetAllStrings().ToDictionary(l => l.Name, l => (object)l.Value));
            await component.SetParametersAsync(parameters);
        }
    }
}
