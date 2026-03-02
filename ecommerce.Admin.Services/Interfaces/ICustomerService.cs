using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Dtos.UserAddressDto;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<IActionResult<Paging<List<CustomerListDto>>>> GetPagedCustomers(PageSetting pager);
        
        /// <summary>
        /// Cari listesini yetki kontrolü olmadan, sadece BranchId filtreleme ile döndürür.
        /// Fatura oluşturma modal'ı gibi yerlerden çağrılır.
        /// </summary>
        Task<IActionResult<Paging<List<CustomerListDto>>>> GetPagedCustomersForInvoice(PageSetting pager);
        Task<IActionResult<CustomerUpsertDto>> GetCustomerById(int id);
        Task<IActionResult<Empty>> UpsertCustomer(CustomerUpsertDto dto);
        Task<IActionResult<Empty>> DeleteCustomer(int id);
        Task<IActionResult<string>> GetNextCustomerCode();

        // Cari - Plasiyer ilişkileri
        Task<IActionResult<List<CustomerSalesPersonDto>>> GetCustomerSalesPersons(int customerId);
        Task<IActionResult<Empty>> AddSalesPersonToCustomer(int customerId, int salesPersonId);
        Task<IActionResult<Empty>> RemoveSalesPersonFromCustomer(int mappingId);
        Task<IActionResult<Empty>> SetDefaultSalesPerson(int customerId, int mappingId);
        
        // Cari - Kullanıcı ilişkileri
        Task<IActionResult<List<IdentityUserListDto>>> GetCustomerUsers(int customerId);
        Task<IActionResult<List<IdentityUserListDto>>> GetAllUsers(int? corporationId = null);
        Task<IActionResult<Empty>> LinkUserToCustomer(int userId, int customerId);
        Task<IActionResult<Empty>> UnlinkUserFromCustomer(int userId);
        
        // Cari - Adres yönetimi (UserAddress via ApplicationUser)
        Task<IActionResult<List<UserAddressListDto>>> GetCustomerAddresses(int customerId);
        Task<IActionResult<UserAddressUpsertDto>> GetCustomerAddressById(int addressId);
        Task<IActionResult<int>> AddCustomerAddress(int customerId, UserAddressUpsertDto address);
        Task<IActionResult<Empty>> UpdateCustomerAddress(int addressId, UserAddressUpsertDto address);
        Task<IActionResult<Empty>> DeleteCustomerAddress(int addressId);
        Task<IActionResult<Empty>> SetDefaultCustomerAddress(int customerId, int addressId);
    }
}
