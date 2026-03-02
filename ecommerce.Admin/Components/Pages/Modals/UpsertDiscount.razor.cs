using AutoMapper;
using ecommerce.Admin.Components.Layout;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Resources;
using ecommerce.Admin.Services;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertDiscount{
        #region Injection
        [Inject] private IJSRuntime JSRuntime{get;set;}
        [Inject] private NavigationManager NavigationManager{get;set;}
        [Inject] private DialogService DialogService{get;set;}
        [Inject] private NotificationService NotificationService{get;set;}
        [Inject] private AuthenticationService Security{get;set;}
        [Inject] private IDiscountService DiscountService{get;set;}
        [Inject] private ICompanyService CompanyService{get;set;}
        [Inject] private ICargoService CargoService{get;set;}
        [Inject] private IProductService ProductService{get;set;}
        [Inject] private ICategoryService CategoryService{get;set;}
        [Inject] private IBrandService BrandService{get;set;}
        [Inject] private IAppSettingService AppSettingService{get;set;}
        [Inject] private IFileService FileService{get;set;}
        [Inject] private FileHelper FileHelper{get;set;}
        [Inject] private IValidator<DiscountUpsertDto> DiscountValidator{get;set;}
        [Inject] private IMapper Mapper{get;set;}
        [Inject] private IStringLocalizer<Culture_TR> RadzenLocalizer{get;set;}
        #endregion
        #region Params
        [Parameter] public int ? Id{get;set;}
        #endregion
        private RadzenFluentValidator<DiscountUpsertDto> DiscountFluentValidator{get;set;}
        private Paging<List<NameValue<int>>> AssignedItems{get;set;} = new();
        private Paging<List<NameValue<int>>> AssignedSellerItems{get;set;} = new();
        private bool AssignedItemsLoading{get;set;}
        private DiscountUpsertDto Discount{get;set;} = new();
        private bool Saving{get;set;}
        private bool LoadingCouponCode{get;set;}
        private Paging<List<ProductListDto>> Products{get;set;} = new();
        private RadzenDataGrid<DiscountCompanyCouponUpsertDto> DiscountCompanyCouponDataGrid{get;set;}
        private DiscountCompanyCouponUpsertDto ? DiscountCompanyCouponToEdit{get;set;}
        private Paging<List<CompanyListDto>> Companies{get;set;} = new();
        private Dictionary<int, CompanyListDto> LoadedCompanies{get;set;} = new();
        protected override async Task OnInitializedAsync(){
            if(Id.HasValue){
                var response = await DiscountService.GetDiscountById(Id.Value);
                if(!response.Ok){
                    NotificationService.Notify(NotificationSeverity.Error, response.GetMetadataMessages());
                    return;
                }
                Discount = response.Result;
                if(Discount.AssignedEntityIds is{Count: > 0}){
                    await LoadAssignedItems(new LoadDataArgs(), Discount.AssignedEntityIds);
                }
                if(Discount.AssignedSellerIds is{Count: > 0}){
                    await LoadAssignedSellerItems(new LoadDataArgs(), Discount.AssignedSellerIds);
                }
                if(Discount.GiftProductIds is{Count: > 0}){
                    await LoadProducts(new LoadDataArgs(), Discount.GiftProductIds);
                }
            }
            Discount.Rule ??= new RuleUpsertDto();
            await InvokeAsync(StateHasChanged);
        }
        private void OnRadzenGridRender<TItem>(DataGridRenderEventArgs<TItem> args){
            if(!args.FirstRender){
                return;
            }
            _ = SetRadzenTexts(args.Grid);
        }
        private async Task SetRadzenTexts(RadzenComponent radzenComponent){
            var parameters = ParameterView.FromDictionary(RadzenLocalizer.GetAllStrings().ToDictionary(l => l.Name, l => (object ?) l.Value));
            await radzenComponent.SetParametersAsync(parameters);
            await InvokeAsync(StateHasChanged);
        }
        private async Task FormSubmit(){
            Saving = true;
            if(Discount.Rule is{Field: null}){
                Discount.Rule = null;
            }
            var submitRs = await DiscountService.UpsertDiscount(Discount);
            if(submitRs.Ok){
                DialogService.Close(Discount);
            } else{
                NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
            }
            Saving = false;
        }
        private async Task SaveImageFile(IBrowserFile file){
            var fileResponse = await FileService.UploadFile(file, "Campaign");
            if(!fileResponse.Ok){
                NotificationService.Notify(NotificationSeverity.Error, fileResponse.GetMetadataMessages());
                return;
            }
            Discount.ImagePath = fileResponse.Result.Root;
        }
        private async Task ShowErrors(){await DialogService.OpenAsync<ValidationModal>("Uyari", new Dictionary<string, object>{{"Errors", DiscountFluentValidator.GetValidationMessages().Select(p => new Dictionary<string, string>{{p.Key, p.Value}}).ToList()}});}
        private void CancelButtonClick(MouseEventArgs args){DialogService.Close();}
        private async Task DiscountTypeChanged(){
            var canHaveGiftProducts = Discount.DiscountType.HasValue && new[]{DiscountType.AssignedToCart, DiscountType.AssignedToCargo}.Contains(Discount.DiscountType.Value);
            Discount.GiftProductIds = canHaveGiftProducts ? Discount.GiftProductIds : new List<int>();
            Discount.HasGiftProducts = canHaveGiftProducts && Discount.HasGiftProducts;
            Discount.GiftProductSellerId = canHaveGiftProducts ? Discount.GiftProductSellerId : null;
            if(Discount.DiscountType.HasValue && !new[]{DiscountType.AssignedToCart}.Contains(Discount.DiscountType.Value)){
                AssignedItemsLoading = true;
                Discount.AssignedEntityIds = new List<int>();
                Discount.AssignedSellerIds = new List<int>();
                await LoadAssignedItems(new LoadDataArgs());
                await LoadAssignedSellerItems(new LoadDataArgs());
                AssignedItemsLoading = false;
            }
        }
        private Task DiscountLimitationTypeChanged(){
            Discount.LimitationTimes = 0;
            return Task.CompletedTask;
        }
        private Task UsePercentageChanged(){
            Discount.DiscountPercentage = null;
            Discount.DiscountAmount = null;
            Discount.MaximumDiscountAmount = null;
            return Task.CompletedTask;
        }
        private async Task LoadAssignedItems(LoadDataArgs args){await LoadAssignedItems(args, null);}
        private async Task LoadAssignedSellerItems(LoadDataArgs args){await LoadAssignedSellerItems(args, null);}
        private async Task LoadAssignedSellerItems(LoadDataArgs args, List<int> ? selectedSellerItems){
            var pager1 = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            if(selectedSellerItems is{Count: > 0}){
                pager1.Skip = 0;
                pager1.Take = selectedSellerItems.Count;
                pager1.Filter = $"new int[] {{ {string.Join(", ", selectedSellerItems)} }}.Contains(Id)";
            }
            if(selectedSellerItems?.Any() != true && !string.IsNullOrEmpty(pager1.Filter) && !pager1.Filter.Contains("Name")){
                pager1.Filter = $"(AccountName != null && AccountName.ToLower().Contains(\"{pager1.Filter.ToLower()}\")) || (FirstName != null && FirstName.ToLower().Contains(\"{pager1.Filter.ToLower()}\")) || (LastName != null && LastName.ToLower().Contains(\"{pager1.Filter.ToLower()}\"))";
            }
            var sellerss = await CompanyService.GetCompanies(pager1);
            if(sellerss.Ok){
                AssignedSellerItems.Data = sellerss.Result.Data.Select(p => new NameValue<int>(p.Id, p.CompanyInformation)).ToList();
                AssignedSellerItems.DataCount = sellerss.Result.DataCount;
                StateHasChanged();
            }
        }
        private async Task LoadAssignedItems(LoadDataArgs args, List<int> ? selectedItems){
            var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            if(selectedItems is{Count: > 0}){
                pager.Skip = 0;
                pager.Take = selectedItems.Count;
                pager.Filter = $"new int[] {{ {string.Join(", ", selectedItems)} }}.Contains(Id)";
            }
            AssignedItems.Data = new List<NameValue<int>>();
            AssignedItems.DataCount = 0;
            switch(Discount.DiscountType){
                case DiscountType.AssignedToProducts:{
                    if(selectedItems?.Any() != true && !string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                        pager.Filter = $"Name.ToLower().Contains(\"{pager.Filter.ToLower()}\")";
                    }
                    var products = await ProductService.GetProducts(pager);
                    if(products.Ok){
                        AssignedItems.Data = products.Result.Data.Select(p => new NameValue<int>(p.Id, p.Name)).ToList();
                        AssignedItems.DataCount = products.Result.DataCount;
                    }
                    break;
                }
                case DiscountType.AssignedToCategories:{
                    if(selectedItems?.Any() != true && !string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                        pager.Filter = $"Name.ToLower().Contains(\"{pager.Filter.ToLower()}\")";
                    }
                    var categories = await CategoryService.GetCategories(pager);
                    if(categories.Ok){
                        AssignedItems.Data = categories.Result.Data.Select(p => new NameValue<int>(p.Id, p.Name)).ToList();
                        AssignedItems.DataCount = categories.Result.DataCount;
                    }
                    break;
                }
                case DiscountType.AssignedToBrands:
                    if(selectedItems?.Any() != true && !string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                        pager.Filter = $"Name.ToLower().Contains(\"{pager.Filter.ToLower()}\")";
                    }
                    var brands = await BrandService.GetBrands(pager);
                    if(brands.Ok){
                        AssignedItems.Data = brands.Result.Data.Select(p => new NameValue<int>(p.Id, p.Name)).ToList();
                        AssignedItems.DataCount = brands.Result.DataCount;
                    }
                    break;
                case DiscountType.AssignedToCargo:
                    if(selectedItems?.Any() != true && !string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                        pager.Filter = $"Name.ToLower().Contains(\"{pager.Filter.ToLower()}\")";
                    }
                    var cargoes = await CargoService.GetCargoes(pager);
                    if(cargoes.Ok){
                        AssignedItems.Data = cargoes.Result.Data.Select(p => new NameValue<int>(p.Id, p.Name)).ToList();
                        AssignedItems.DataCount = cargoes.Result.DataCount;
                    }
                    break;
            }
            await InvokeAsync(StateHasChanged);
        }
        private async Task LoadProducts(LoadDataArgs args){await LoadProducts(args, null);}
        private async Task LoadProducts(LoadDataArgs args, List<int> ? selectedItems){
            var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            if(selectedItems is{Count: > 0}){
                pager.Skip = 0;
                pager.Take = selectedItems.Count;
                pager.Filter = $"new int[] {{ {string.Join(", ", selectedItems)} }}.Contains(Id)";
            } else
                if(!string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                    pager.Filter = $"Name.ToLower().Contains(\"{pager.Filter.ToLower()}\")";
                }
            var products = await ProductService.GetProducts(pager);
            if(products.Ok){
                Products = products.Result;
            }
            await InvokeAsync(StateHasChanged);
        }
        private async Task LoadCompanies(LoadDataArgs args){
            var pager = new PageSetting(args.Filter, args.OrderBy, args.Skip, args.Top);
            if(!string.IsNullOrEmpty(pager.Filter) && !pager.Filter.Contains("Name")){
                var filter = pager.Filter.ToLower();
                pager.Filter = $"FirstName.ToLower().Contains(\"{filter}\") or LastName.ToLower().Contains(\"{filter}\") or GlnNumber.ToLower().Contains(\"{filter}\")";
            }
            if(LoadedCompanies.Count == 0){
                var initialSelectedItems = Discount.CompanyCoupons.Select(x => x.CompanyId).ToList();
                if(Discount.GiftProductSellerId.HasValue){
                    initialSelectedItems.Add(Discount.GiftProductSellerId.Value);
                }
                if(initialSelectedItems.Count > 0){
                    pager.Skip = 0;
                    pager.Take = initialSelectedItems.Count;
                    pager.Filter = $"new int[] {{ {string.Join(", ", initialSelectedItems)} }}.Contains(Id)";
                }
            }
            var companies = await CompanyService.GetCompanies(pager);
            if(companies.Ok){
                Companies = new Paging<List<CompanyListDto>>{Data = companies.Result.Data?.ToList() ?? new List<CompanyListDto>(), DataCount = companies.Result.DataCount};
                foreach(var company in Companies.Data){
                    LoadedCompanies.TryAdd(company.Id, company);
                }
            }
            await InvokeAsync(StateHasChanged);
        }
        private async Task InsertDiscountCompanyCouponRow(){
            if(DiscountCompanyCouponToEdit != null){
                DiscountCompanyCouponDataGrid.CancelEditRow(DiscountCompanyCouponToEdit);
            }
            DiscountCompanyCouponToEdit = new DiscountCompanyCouponUpsertDto{CouponCode = await GenerateCouponCode()};
            await DiscountCompanyCouponDataGrid.InsertRow(DiscountCompanyCouponToEdit);
        }
        private async Task EditDiscountCompanyCouponRow(DiscountCompanyCouponUpsertDto dto){
            DiscountCompanyCouponToEdit = Mapper.Map<DiscountCompanyCouponUpsertDto>(dto);
            await DiscountCompanyCouponDataGrid.EditRow(dto);
        }
        private async Task SaveDiscountCompanyCouponRow(DiscountCompanyCouponUpsertDto dto){
            var company = LoadedCompanies.GetValueOrDefault(DiscountCompanyCouponToEdit.CompanyId);
            if(company == null){
                return;
            }
            DiscountCompanyCouponToEdit.Company = company;
            if(dto == DiscountCompanyCouponToEdit){
                Discount.CompanyCoupons.Add(dto);
            } else{
                Mapper.Map(DiscountCompanyCouponToEdit, dto);
            }
            await DiscountCompanyCouponDataGrid.UpdateRow(dto);
        }
        private async Task DeleteDiscountCompanyCouponRow(DiscountCompanyCouponUpsertDto dto){
            Discount.CompanyCoupons.Remove(dto);
            await DiscountCompanyCouponDataGrid.Reload();
        }
        private void CancelDiscountCompanyCouponEdit(DiscountCompanyCouponUpsertDto dto){DiscountCompanyCouponDataGrid.CancelEditRow(dto);}
        private async Task<string> GenerateCouponCode(){
            LoadingCouponCode = true;
            var result = await DiscountService.GenerateCouponCode();
            LoadingCouponCode = false;
            return result;
        }
    }
}
