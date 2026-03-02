using ecommerce.Admin.Services.Concreate;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace ecommerce.Admin.Components.Layout
{
    public partial class BranchIndicator : IDisposable
    {
        [Inject] private BranchSwitcherService BranchSwitcher { get; set; } = default!;
        [Inject] private NotificationService NotificationService { get; set; } = default!;

        private bool isMultiTenant = false;
        private (int Id, string Name)? currentBranch;
        private List<(int Id, string Name)> userBranches = new();
        private bool isDropdownOpen = false;

        protected override async Task OnInitializedAsync()
        {
            await LoadBranchData();
            BranchSwitcher.OnBranchChanged += OnBranchChangedHandler;
        }

        private async Task LoadBranchData()
        {
            try
            {
                currentBranch = await BranchSwitcher.GetCurrentBranchAsync();
                isMultiTenant = currentBranch.HasValue;

                if (isMultiTenant)
                {
                    userBranches = await BranchSwitcher.GetUserBranchesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading branch data: {ex.Message}");
                isMultiTenant = false;
            }
        }

        private void ToggleDropdown()
        {
            isDropdownOpen = !isDropdownOpen;
        }

        private void CloseDropdown()
        {
            isDropdownOpen = false;
        }

        private async Task SwitchBranch(int branchId)
        {
            try
            {
                var success = await BranchSwitcher.SwitchBranchAsync(branchId);
                
                if (success)
                {
                    var branchName = userBranches.FirstOrDefault(b => b.Id == branchId).Name;
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Success,
                        Summary = "Şube Değiştirildi",
                        Detail = $"{branchName} şubesine geçildi",
                        Duration = 3000
                    });
                    
                    isDropdownOpen = false;
                }
                else
                {
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Hata",
                        Detail = "Şube değiştirilemedi",
                        Duration = 4000
                    });
                }
            }
            catch (Exception ex)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Hata",
                    Detail = $"Şube değiştirme hatası: {ex.Message}",
                    Duration = 4000
                });
            }
        }

        private void OnBranchChangedHandler()
        {
            InvokeAsync(async () =>
            {
                await LoadBranchData();
                StateHasChanged();
            });
        }

        public void Dispose()
        {
            BranchSwitcher.OnBranchChanged -= OnBranchChangedHandler;
        }
    }
}
