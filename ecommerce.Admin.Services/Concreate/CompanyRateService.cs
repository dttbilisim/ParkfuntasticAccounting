using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyRateDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class CompanyRateService : ICompanyRateService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<CompanyRate> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public CompanyRateService(
            IUnitOfWork<ApplicationDbContext> context,
            IMapper mapper,
            ILogger logger)
        {
            _context = context;
            _repository = context.GetRepository<CompanyRate>();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult<Empty>> DeleteCompanyRate(AuditWrapDto<CompanyRateDeleteDto> model)
        {

            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                await _context.DbContext.CompanyRate.Where(f => f.Id == model.Dto.Id)
      .ExecuteUpdateAsync(x => x.SetProperty(x => x.DeletedId, model.UserId)
      .SetProperty(x => x.Status, EntityStatus.Deleted.GetHashCode()));


                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }

                if (lastResult != null && lastResult.Exception != null)
                    response.AddError(lastResult.Exception.ToString());

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteCompanyRate Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }

        }

        public async Task<IActionResult<List<CompanyRateListDto>>> GetCompanyRateByCompanyId(int companyId)
        {
            IActionResult<List<CompanyRateListDto>> response = new IActionResult<List<CompanyRateListDto>> { Result = new() };
            try
            {
                var companyRates = _repository.GetAll(
                    predicate: f => f.CompanyId == companyId && f.Status == (int)EntityStatus.Active, include: x =>
                    x.Include(rate => rate.Product).Include(rate => rate.Category).Include(rate => rate.Tier));


                var mapped = _mapper.Map<List<CompanyRateListDto>>(companyRates);
                if (mapped != null)
                {
                    response.Result = mapped.ToList();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyRateByCompanyId Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CompanyRateListDto>>> GetCompanyRateByProductId(int productId)
        {
            IActionResult<List<CompanyRateListDto>> response = new IActionResult<List<CompanyRateListDto>> { Result = new() };
            try
            {
                IList<CompanyRate> companyRates = await _repository.GetAllAsync(predicate: f => f.ProductId == productId);
                var mapped = _mapper.Map<List<CompanyRateListDto>>(companyRates);
                if (mapped != null)
                {
                    response.Result = mapped.ToList();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyRateByCompanyId Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<CompanyRateUpsertDto>> GetCompanyRateById(int Id)
        {
            IActionResult<CompanyRateUpsertDto> response = new IActionResult<CompanyRateUpsertDto> { Result = new() };
            try
            {
                var product = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<CompanyRateUpsertDto>(product);
                if (mapped != null)
                {
                    response.Result = mapped;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyRateById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCompanyRate(AuditWrapDto<CompanyRateUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<CompanyRate>(dto);
                if (!dto.Id.HasValue)
                {
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    entity = await _context.DbContext.CompanyRate.FirstOrDefaultAsync(x => x.Id == dto.Id);

                    entity.CompanyId = dto.CompanyId;
                    entity.ProductId = dto.ProductId;
                    entity.CategoryId = dto.CategoryId;
                    entity.TierId = dto.TierId;
                    entity.Rate = dto.Rate.Value;

                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.ModifiedId = model.UserId;
                    entity.ModifiedDate = DateTime.Now;
                    _repository.Update(entity);
                }

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpsertCompanyRate Exception {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
