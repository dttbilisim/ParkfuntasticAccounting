using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertProductMerge{
    [Inject] protected NavigationManager NavigationManager{get;set;}
    [Inject] protected DialogService DialogService{get;set;}
    [Inject] protected TooltipService TooltipService{get;set;}
    [Inject] protected ContextMenuService ContextMenuService{get;set;}
    [Inject] protected NotificationService NotificationService{get;set;}
    [Inject] public IProductService _productService{get;set;}
    [Inject] public IBrandService BrandService{get;set;}
    [Inject] public IProductTypeService ProductTypeService{get;set;}
    [Inject] public IJSRuntime _JsRuntime{get;set;}
    protected List<ProductDublicaListDto> products = new();
    private MergeProductUpsertDto _mergeProductUpsertDto = new();
    protected override async Task OnAfterRenderAsync(bool firstRender){
        if(firstRender){
            await GetProductData();
        }
    }
    private async Task GetProductData(){
        try{
            var rs = await _productService.GetDublicateProductListWitGroup();
            if(rs.Ok){
                products = rs.Result;
                StateHasChanged();
            }
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error, "Urun listesini alinamadi");
        }
    }
    private async Task MergeAndReplaceProductAsync(){
        try{
            var rs = await _productService.MergeProductsAsync(_mergeProductUpsertDto);
            if(rs.Ok){
                await GetProductData();
                NotificationService.Notify(NotificationSeverity.Success, rs.Metadata.Message);
            } else{
                NotificationService.Notify(NotificationSeverity.Error, rs.Metadata.Message);
            }
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error, e.Message);
        }
    }
}
