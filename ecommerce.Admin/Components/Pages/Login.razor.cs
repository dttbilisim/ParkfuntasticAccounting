using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Radzen;
using ecommerce.Admin.Services;

namespace ecommerce.Admin.Components.Pages
{
    public partial class Login
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

        protected string redirectUrl;
        protected string error;
        protected string info;
        protected bool errorVisible;
        protected bool infoVisible;
        protected bool isBusy;

        [Inject]
        protected AuthenticationService Security { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var query = System.Web.HttpUtility.ParseQueryString(new Uri(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).ToString()).Query);
   
            var rawError = query.Get("error");
            var rawInfo = query.Get("info");

            // Parse error if it's JSON
            if (!string.IsNullOrEmpty(rawError))
            {
                error = ParseErrorMessage(rawError);
            }

            // Parse info if it's JSON
            if (!string.IsNullOrEmpty(rawInfo))
            {
                info = ParseErrorMessage(rawInfo);
            }

            redirectUrl = query.Get("redirectUrl");

            errorVisible = !string.IsNullOrEmpty(error);

            infoVisible = !string.IsNullOrEmpty(info);
        }

        private string ParseErrorMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            // Try to parse as JSON
            try
            {
                // Check if it looks like JSON
                if (message.TrimStart().StartsWith("{") || message.TrimStart().StartsWith("["))
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(message);
                    
                    // Try to extract common error message fields
                    if (jsonDoc.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        return messageElement.GetString() ?? message;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        return errorElement.GetString() ?? message;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("title", out var titleElement))
                    {
                        return titleElement.GetString() ?? message;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("detail", out var detailElement))
                    {
                        return detailElement.GetString() ?? message;
                    }
                }
            }
            catch
            {
                // If parsing fails, return the original message
            }

            return message;
        }
         protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // ONLY hide loader - nothing else
                try 
                {
                   await JSRuntime.InvokeVoidAsync("hideAppLoader");
                }
                catch { }
            }
            
            await base.OnAfterRenderAsync(firstRender);
        }
    }
}