using ecommerce.Core.Entities;

namespace ecommerce.Domain.Shared.Rules.Providers
{
    public static class PopupFieldDefinitions
    {
        public const string Scope = nameof(Popup);
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
        public const string UserBirthday = "UserBirthday";
        public const string Company = "Company";
        public const string CompanyType = "CompanyType";
        public const string IsAuthenticated = "IsAuthenticated";
    }
}