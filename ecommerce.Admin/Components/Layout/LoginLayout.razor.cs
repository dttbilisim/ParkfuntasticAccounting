using ecommerce.Admin.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace ecommerce.Admin.Components.Layout
{
    public partial class LoginLayout
    {
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

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
       
    }
}