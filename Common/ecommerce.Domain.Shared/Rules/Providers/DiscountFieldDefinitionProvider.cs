using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Rules.OptionProviders;
using ecommerce.Domain.Shared.Rules.ValueProviders;

namespace ecommerce.Domain.Shared.Rules.Providers
{
    public class DiscountFieldDefinitionProvider : FieldDefinitionProvider
    {
        public override void Define(IFieldDefinitionContext context)
        {
            var scope = context.AddScope(DiscountFieldDefinitions.Scope, "Discount");

            scope.AddField(DiscountFieldDefinitions.CartTotal, typeof(decimal), typeof(CartTotalValueProvider), "Sepet Toplam Tutar");

            scope.AddField(DiscountFieldDefinitions.CartSubtotal, typeof(decimal), typeof(CartSubtotalValueProvider), "Sepet Ara Toplam");

            // scope.AddField(DiscountFieldDefinitions.CartProductCount, typeof(int), typeof(CartProductCountValueProvider), "Sepet Ürün Sayısı");

            scope.AddField(DiscountFieldDefinitions.CartItemQuantity, typeof(int), typeof(CartItemQuantityValueProvider), "Sepet Ürün Adedi");

            scope.AddField(DiscountFieldDefinitions.ProductInCart, typeof(int[]), typeof(ProductInCartValueProvider), "Sepetteki Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));

            scope.AddField(DiscountFieldDefinitions.ProductFromCategoryInCart, typeof(int[]), typeof(ProductFromCategoryInCartValueProvider), "Sepetteki Belirli Kategorideki Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(CategoryOptionProvider)));

            scope.AddField(DiscountFieldDefinitions.ProductFromBrandInCart, typeof(int[]), typeof(ProductFromBrandInCartValueProvider), "Sepetteki Belirli Markadaki Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(BrandOptionProvider)));

            // scope.AddField(DiscountFieldDefinitions.ProductFromSellerInCart, typeof(int[]), typeof(ProductFromSellerInCartValueProvider), "Sepetteki Belirli Satıcıdan Ürünler")
            //     .WithSelectList(new FieldDefinitionValueSelectList(typeof(CompanyOptionProvider)));
           
            // scope.AddField(DiscountFieldDefinitions.CargoInCart, typeof(int[]), typeof(CargoFromValueProvider), "Belirli Kargo Firmalarına")
            //     .WithSelectList(new FieldDefinitionValueSelectList(typeof(CargoOptionProvider)));

            // scope.AddField(DiscountFieldDefinitions.OrderCount, typeof(int), typeof(OrderCountValueProvider), "Toplam Verilen Sipariş");

            // scope.AddField(DiscountFieldDefinitions.OrderSpentAmount, typeof(decimal), typeof(OrderSpentAmountValueProvider), "Toplam Harcanan Tutar");

            // scope.AddField(DiscountFieldDefinitions.OrderPurchasedProducts, typeof(int[]), typeof(OrderPurchasedProductsValueProvider), "Satın Aldığı Ürünler")
                // .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));

            // scope.AddField(DiscountFieldDefinitions.OrderSalesCount, typeof(int), typeof(OrderSalesCountValueProvider), "Toplam Alınan Sipariş");

            // scope.AddField(DiscountFieldDefinitions.OrderSalesAmount, typeof(decimal), typeof(OrderSalesAmountValueProvider), "Toplam Satış Tutarı");

            // scope.AddField(DiscountFieldDefinitions.OrderSoldProducts, typeof(int[]), typeof(OrderPurchasedProductsValueProvider), "Sattığı Ürünler")
                // .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));

            scope.AddField(DiscountFieldDefinitions.Today, typeof(DateOnly), typeof(TodayValueProvider), "Bugunün Tarihi");

            scope.AddField(DiscountFieldDefinitions.Weekday, typeof(int), typeof(WeekdayValueProvider), "Haftanın Günü")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(WeekdayOptionProvider)));

            // scope.AddField(DiscountFieldDefinitions.Seller, typeof(int[]), typeof(SellerValueProvider), "Satıcılar")
            //     .WithSelectList(new FieldDefinitionValueSelectList(typeof(SellerOptionProvider)));

            // scope.AddField(DiscountFieldDefinitions.UserSignUpDate, typeof(DateTime), typeof(UserSignUpDateValueProvider), "Kullanıcı Kayıt Tarihi");

            scope.AddField(DiscountFieldDefinitions.Company, typeof(int[]), typeof(CompanyValueProvider), "Kullanıcı")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(CompanyOptionProvider)));

            scope.AddField(DiscountFieldDefinitions.CompanyType, typeof(UserType), typeof(CompanyTypeValueProvider), "Kullanıcı Tipi");

            // scope.AddField(DiscountFieldDefinitions.PharmacyType, typeof(int[]), typeof(PharmacyTypeValueProvider), "Eczane Tipi")
            //     .WithSelectList(new FieldDefinitionValueSelectList(typeof(PharmacyTypeOptionProvider)));
        }
    }
}