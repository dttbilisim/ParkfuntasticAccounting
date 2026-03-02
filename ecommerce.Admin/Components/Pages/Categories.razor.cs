using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Services;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages{
    public partial class Categories{
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public ICategoryService Service{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}

        protected List<Category> HierarchicalCategories { get; set; } = null;
        private new DialogOptions DialogOptions = new(){Width = "900px"};

        protected override async Task OnInitializedAsync()
        {
            await LoadHierarchy();
        }

        protected async Task LoadHierarchy()
        {
             HierarchicalCategories = null; // Loading state
             StateHasChanged();
             
             var response = await Service.GetCategoryHierarchy();
             if (response.Ok)
             {
                 HierarchicalCategories = response.Result;
                 Console.WriteLine($"DEBUG: Categories.razor - Loaded Count: {HierarchicalCategories?.Count ?? 0}");
             }
             else
             {
                 NotificationService.Notify(NotificationSeverity.Error, "Hata", response.GetMetadataMessages());
             }
             StateHasChanged();
        }

        protected async Task AddButtonClick(MouseEventArgs args){
            // Use UpsertCategory (standard)
            await DialogService.OpenAsync<UpsertCategory>("Kategori Ekle", new Dictionary<string, object> { { "Id", null } }, DialogOptions);
            await LoadHierarchy();
        }

        protected async Task EditCategory(Category category){
             // Id needed for Upsert. Category entity has Id.
            await DialogService.OpenAsync<UpsertCategory>("Kategori Düzenle", new Dictionary<string, object>{{"Id", category.Id}}, DialogOptions);
            await LoadHierarchy();
        }

        protected async Task DeleteCategory(Category category){
            try{
                if(await DialogService.Confirm("Seçilen Kategoriyi silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await Service.DeleteCategory(new Core.Helpers.AuditWrapDto<CategoryDeleteDto>(){UserId = Security.User.Id, Dto = new CategoryDeleteDto(){Id = category.Id}});
                    if(deleteResult != null){
                        if(deleteResult.Ok)
                        {
                            NotificationService.Notify(NotificationSeverity.Success, "Başarılı", "Kategori silindi.");
                            await LoadHierarchy();
                        }
                        else
                        {
                            await DialogService.Alert(deleteResult.Metadata.Message, "Uyarı", new AlertOptions(){OkButtonText = "Tamam"});
                        }
                    }
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete Category"});
            }
        }
    }
}
