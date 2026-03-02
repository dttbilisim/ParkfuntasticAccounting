using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Core.Utils;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;

namespace ecommerce.Admin.Components.Pages.Modals
{
    public partial class FilterProduct
    {

        [Parameter]
        public string GeneratedFilter { get; set; }
        [Inject] protected DialogService DialogService { get; set; }
        [Inject] protected NotificationService NotificationService { get; set; }


        protected bool errorVisible;
        //public EntityStatusForFilter? Status { get; set; }

        public ProductCustomFilterDto _filter = new();

        protected override async Task OnInitializedAsync()
        {
            if (!string.IsNullOrEmpty(GeneratedFilter))
            {
                ParseFilter(GeneratedFilter);
            }
        }
        protected async Task FormSubmit()
        {
            try
            {
                GeneratedFilter = GenerateFilter();
                DialogService.Close(GeneratedFilter);
            }
            catch (Exception ex)
            {
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }

        public string GenerateFilter()
        {
            List<string> conditions = new List<string>();

            if (_filter.Status != null)
            {
                var StatusInt = (bool)_filter.Status ? 1 : 0;
                conditions.Add($"Status == {StatusInt}");
            }

            if (_filter.ProductsWithoutImage != null)
            {
                conditions.Add($"ProductsWithoutImage == {_filter.ProductsWithoutImage}");
            }

            if (_filter.ProductsWithoutCategory != null)
            {
                conditions.Add($"ProductsWithoutCategory == {_filter.ProductsWithoutCategory}");
            }

            // Yeni özelliklerin koşulları buraya eklenebilir

            if (conditions.Count > 0)
            {
                return string.Join(" and ", conditions);
            }
            else
            {
                return string.Empty;
            }
        }

        public void ParseFilter(string filter)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                string[] conditions = filter.Split(new string[] { " and " }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var condition in conditions)
                {
                    string[] parts = condition.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        var propertyName = parts[0].Trim();
                        var propertyValue = parts[1].Trim();

                        switch (propertyName)
                        {
                            case "Status":
                                if (int.TryParse(propertyValue, out var statusValue))
                                {
                                    _filter.Status = statusValue == 1;
                                }
                                break;
                            case "ProductsWithoutImage":
                                if (bool.TryParse(propertyValue, out var withoutImageValue))
                                {
                                    _filter.ProductsWithoutImage = withoutImageValue;
                                }
                                break;
                            case "ProductsWithoutCategory":
                                if (bool.TryParse(propertyValue, out var withoutCategoryValue))
                                {
                                    _filter.ProductsWithoutCategory = withoutCategoryValue;
                                }
                                break;
                            // Yeni özellikler eklenebilir
                            default:
                                // Bilinmeyen bir özellik varsa burada işlem yapılabilir
                                break;
                        }
                    }
                }
            }
        }

        protected void ClearButtonClick(MouseEventArgs args)
        {
            GeneratedFilter = string.Empty;
            DialogService.Close(GeneratedFilter);
        }

        protected async Task TabChange(int index)
        {

        }
    }
}

