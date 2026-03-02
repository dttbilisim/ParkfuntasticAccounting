using Microsoft.AspNetCore.Components;

namespace ecommerce.Web.Components.Pages.Components;

public partial class BreadcrumbComponent 
{
    [Parameter] [SupplyParameterFromQuery] public string BreadcrumbText { get; set; }
}