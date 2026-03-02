using ecommerce.Core.Rules.Fields;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Rules.OptionProviders;
using ecommerce.Domain.Shared.Rules.ValueProviders;

namespace ecommerce.Domain.Shared.Rules.Providers
{
    public class PopupFieldDefinitionProvider : FieldDefinitionProvider
    {
        public override void Define(IFieldDefinitionContext context)
        {
            var scope = context.AddScope(PopupFieldDefinitions.Scope, "Popup");

            scope.AddField(PopupFieldDefinitions.OrderCount, typeof(int), typeof(OrderCountValueProvider), "Toplam Verilen Sipariş");

            scope.AddField(PopupFieldDefinitions.OrderSpentAmount, typeof(decimal), typeof(OrderSpentAmountValueProvider), "Toplam Harcanan Tutar");

            scope.AddField(PopupFieldDefinitions.OrderPurchasedProducts, typeof(int[]), typeof(OrderPurchasedProductsValueProvider), "Satın Aldığı Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));

            scope.AddField(PopupFieldDefinitions.OrderSalesCount, typeof(int), typeof(OrderSalesCountValueProvider), "Toplam Alınan Sipariş");

            scope.AddField(PopupFieldDefinitions.OrderSalesAmount, typeof(decimal), typeof(OrderSalesAmountValueProvider), "Toplam Satış Tutarı");

            scope.AddField(PopupFieldDefinitions.OrderSoldProducts, typeof(int[]), typeof(OrderPurchasedProductsValueProvider), "Sattığı Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));
            
            scope.AddField(PopupFieldDefinitions.OrderSoldProducts, typeof(int[]), typeof(OrderPurchasedProductsValueProvider), "Sattığı Ürünler")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(ProductOptionProvider)));
            
          

            scope.AddField(PopupFieldDefinitions.Today, typeof(DateOnly), typeof(TodayValueProvider), "Bugunün Tarihi");

            scope.AddField(PopupFieldDefinitions.Weekday, typeof(int), typeof(WeekdayValueProvider), "Haftanın Günü")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(WeekdayOptionProvider)));

            scope.AddField(PopupFieldDefinitions.User, typeof(int[]), typeof(UserValueProvider), "Kullanıcı")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(UserOptionProvider)));

            scope.AddField(PopupFieldDefinitions.UserSignUpDate, typeof(DateTime), typeof(UserSignUpDateValueProvider), "Kullanıcı Kayıt Tarihi");

            scope.AddField(PopupFieldDefinitions.UserBirthday, typeof(DateOnly), typeof(UserBirthdayValueProvider), "Kullanıcı Doğum Tarihi");

            scope.AddField(PopupFieldDefinitions.Company, typeof(int[]), typeof(CompanyValueProvider), "Şirket")
                .WithSelectList(new FieldDefinitionValueSelectList(typeof(CompanyOptionProvider)));

            scope.AddField(PopupFieldDefinitions.CompanyType, typeof(UserType), typeof(CompanyTypeValueProvider), "Şirket Tipi");

            scope.AddField(PopupFieldDefinitions.IsAuthenticated, typeof(bool), typeof(IsAuthenticatedValueProvider), "Kullanıcı Girişi");
        }
    }
}