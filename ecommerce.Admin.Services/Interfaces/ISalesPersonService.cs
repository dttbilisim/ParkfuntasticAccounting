using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.MonthDto;
using ecommerce.Admin.Domain.Dtos.CustomerWorkPlanDto;
// using ecommerce.Admin.Domain.Dtos.Plasiyer; // Plasiyer rota devre dışı
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ISalesPersonService
    {
        Task<IActionResult<Paging<IQueryable<SalesPersonListDto>>>> GetSalesPersons(PageSetting pager);
        Task<IActionResult<List<SalesPersonListDto>>> GetSalesPersons();
        Task<IActionResult<Empty>> UpsertSalesPerson(AuditWrapDto<SalesPersonUpsertDto> model);
        Task<IActionResult<Empty>> DeleteSalesPerson(AuditWrapDto<SalesPersonDeleteDto> model);
        Task<IActionResult<SalesPersonUpsertDto>> GetSalesPersonById(int id);
        Task<IActionResult<List<CustomerListDto>>> GetCustomersByRegion(int regionId);
        Task<IActionResult<List<CustomerListDto>>> GetCustomersOfSalesPerson(int salesPersonId, string? search = null);
        Task<IActionResult<List<CustomerWithCoordsDto>>> GetCustomersWithCoordsOfSalesPerson(int salesPersonId);
        Task<IActionResult<Empty>> AssignCustomersToSalesPerson(int salesPersonId, int regionId);
        Task<IActionResult<List<MonthListDto>>> GetMonths();
        Task<IActionResult<List<CustomerWorkPlanListDto>>> GetWorkPlansBySalesPerson(int salesPersonId);
        Task<IActionResult<Empty>> UpsertWorkPlan(AuditWrapDto<CustomerWorkPlanUpsertDto> model);
        Task<IActionResult<Empty>> DeleteWorkPlan(AuditWrapDto<CustomerWorkPlanDeleteDto> model);
        // Plasiyer rota - şimdilik devre dışı
        // Task<IActionResult<List<PlasiyerRotaCustomerDto>>> GetPlasiyerRotaList(int salesPersonId);
        // Task<IActionResult<List<PlasiyerCustomerVisitDto>>> GetCustomerVisitDetails(int customerId, int salesPersonId);
        // Task<IActionResult<Empty>> SaveCustomerVisit(int customerId, int salesPersonId, string visitNote);
    }
}
