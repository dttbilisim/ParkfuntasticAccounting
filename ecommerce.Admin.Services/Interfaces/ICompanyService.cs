using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces{
    public interface ICompanyService{
        Task<IActionResult<Paging<List<CompanyListDto>>>> GetCompanies(PageSetting pager);
        Task<IActionResult<List<CompanyListDto>>> GetCompanies();
        Task<IActionResult<List<CompanyListDto>>> GetCompanies(List<int> Ids);
        Task<IActionResult<List<CompanyListDto>>> GetSellerCompanies();
        Task<IActionResult<int>> UpsertCompany(AuditWrapDto<CompanyUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCompany(AuditWrapDto<CompanyDeleteDto> model);
        Task<IActionResult<CompanyUpsertDto>> GetCompanyById(int Id);
        Task<IActionResult<List<ReportStorage>>> GetReportList();
        Task<IActionResult<Paging<List<PharmacyDataDto>>>> GetPharmacyData(PageSetting pager);
        Task<IActionResult<string>> UploadPharmactData(PharmacyData model);
        Task<IActionResult<List<CompanyDocumentListDto>>> GetCompanyDocumentList(string email);

        //Warehouser process
        Task<IActionResult<List<CompanyWarehouseListDto>>> GetCompanyWarehouseList(int companyId);
        Task<IActionResult<Empty>> DeleteCompanyWarehouse(AuditWrapDto<CompanyWarehouseDeleteDto> model);
        Task<IActionResult<Empty>> UpsertCompanyWarehouse(CompanyWareHouse model);

        //seller products
        Task<IActionResult<List<ProductSellerItem>>> GetSellerproducts(int ? sellerId);
        Task<IActionResult<string>> UpsertCompanyInterview(CompanyInterviewDto model);
        Task<IActionResult<List<CompanyInterviewDto>>> GetCompanyInterview(int companyId);
        Task<IActionResult<string>> DeleteCompanyInterView(int Id);
    }
}
