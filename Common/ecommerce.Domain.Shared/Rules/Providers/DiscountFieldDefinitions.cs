using ecommerce.Core.Entities;

namespace ecommerce.Domain.Shared.Rules.Providers
{
    public static class DiscountFieldDefinitions
    {
        public const string Scope = nameof(Discount);
        public const string CartItemQuantity = "CartItemQuantity";
        public const string CartTotal = "CartTotal";
        public const string CartSubtotal = "CartSubtotal";
        public const string CartProductCount = "CartProductCount";
        public const string ProductInCart = "ProductInCart";
        public const string ProductFromCategoryInCart = "ProductFromCategoryInCart";
        public const string ProductFromBrandInCart = "ProductFromBrandInCart";
        public const string ProductFromSellerInCart = "ProductFromSellerInCart";
        public const string CargoInCart = "CargoInCart";
        public const string OrderCount = "OrderCount";
        public const string OrderSpentAmount = "OrderSpentAmount";
        public const string OrderPurchasedProducts = "OrderPurchasedProducts";
        public const string OrderSalesCount = "OrderSalesCount";
        public const string OrderSalesAmount = "OrderSalesAmount";
        public const string OrderSoldProducts = "OrderSoldProducts";
        public const string Weekday = "Weekday";
        public const string Today = "Today";
        public const string User = "User";
        public const string UserSignUpDate = "UserSignUpDate";
        public const string Seller = "Seller";
        public const string Company = "Company";
        public const string CompanyType = "CompanyType";
        public const string PharmacyType = "PharmacyType";
    }
}