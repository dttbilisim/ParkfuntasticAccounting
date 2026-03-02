using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ecommerce.Admin.Services.Dtos.LogDto;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages;

public partial class SystemLogs : ComponentBase
{
    [Inject] DialogService DialogService { get; set; }

    RadzenDataGrid<LogDto> grid;
    List<LogDto> logs;
    int totalLogs;
    bool isLoading;
    string searchTerm;
    string selectedLevel = "All";
    string selectedApplication = "All";
    
    List<string> logLevels = new List<string> { "All", "Information", "Warning", "Error", "Fatal", "Debug" };
    List<string> applications = new List<string> { "All", "Admin", "Web", "Mobile" };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    async Task LoadLogs(LoadDataArgs args)
    {
        isLoading = true;
        try
        {
            var page = args.Skip.HasValue ? args.Skip.Value / args.Top.Value : 0;
            var pageSize = args.Top ?? 20;

            var result = await LogService.GetLogsAsync(page, pageSize, selectedLevel, searchTerm, selectedApplication);
            logs = result.Logs;
            totalLogs = (int)result.Total;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }

    async Task RefreshLogs()
    {
        await grid.Reload();
    }

    async Task OnSearch(string value)
    {
        searchTerm = value;
        await grid.Reload();
    }

    async Task OnLevelChange()
    {
        await grid.Reload();
    }

    async Task OnApplicationChange()
    {
        await grid.Reload();
    }

    BadgeStyle GetBadgeStyle(string level)
    {
        return level switch
        {
            "Error" => BadgeStyle.Danger,
            "Fatal" => BadgeStyle.Danger,
            "Warning" => BadgeStyle.Warning,
            "Information" => BadgeStyle.Info,
            "Debug" => BadgeStyle.Secondary,
            _ => BadgeStyle.Light
        };
    }

    async Task ShowDetails(LogDto log)
    {
        await DialogService.OpenAsync<ecommerce.Admin.Components.Pages.Modals.LogDetailModal>("Log Detayı", 
            new Dictionary<string, object> { { "Log", log } },
            new DialogOptions { Width = "800px", Height = "600px", Resizable = true, Draggable = true });
    }
}
