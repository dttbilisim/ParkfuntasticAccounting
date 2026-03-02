namespace ecommerce.Core.Interfaces
{
    public interface ITenantProvider
    {
        bool IsGlobalAdmin { get; }
        bool IsB2BAdmin { get; }      // B2BADMIN role check
        bool IsPlasiyer { get; }       // Plasiyer role check
        bool IsCustomerB2B { get; }    // CustomerB2B role check
        
        int? GetSalesPersonId();       // For Plasiyer filtering
        int? GetCustomerId();          // For CustomerB2B filtering
        
        int GetCurrentBranchId();
        int GetCurrentCorporationId();
        bool IsMultiTenantEnabled { get; }
        void SetActiveBranchId(int branchId);
    }
}
