using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class CompanyCargoService : ICompanyCargoService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<CompanyCargo> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<CompanyCargoListDto> _radzenPagerService;

        public CompanyCargoService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<CompanyCargoListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<CompanyCargo>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }

        public async Task<IActionResult<Empty>> DeleteCompanyCargo(AuditWrapDto<CompanyCargoDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.CompanyCargoes.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Silme ??lemi Ba?ar?l?");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteCompanyCargo Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<CompanyCargoUpsertDto>> GetCompanyCargoById(int Id)
        {
            var rs = new IActionResult<CompanyCargoUpsertDto>
            {
                Result = new()
            };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<CompanyCargoUpsertDto>(data);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyCargoById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<CompanyCargoListDto>>>> GetCompanyCargoes(PageSetting pager, int sellerId)
        {
            IActionResult<Paging<IQueryable<CompanyCargoListDto>>> response = new() { Result = new() };

            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == (int)EntityStatus.Active && f.SellerId==sellerId);
                var mapped = _mapper.Map<List<CompanyCargoListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        response.Result.Data = mapped.AsQueryable();
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);

                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyCargoes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CompanyCargoListDto>>> GetCompanyCargoes(int sellerId)
        {
            var rs = new IActionResult<List<CompanyCargoListDto>>
            {
                Result = new List<CompanyCargoListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == 1 && f.SellerId == sellerId,include:x=>x.Include(f=>f.Cargo));
                var mapped = _mapper.Map<List<CompanyCargoListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCompanyCargos Exception " + ex.ToString());

                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCompanyCargo(AuditWrapDto<CompanyCargoUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                if (model.Dto.IsDefault)
                {
                    var cargo = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Status == (int)EntityStatus.Active && x.IsDefault == true && x.SellerId==model.Dto.SellerId,disableTracking:false);
                    if(cargo is not null)
                      {
                        cargo.IsDefault = false;
                        await _context.SaveChangesAsync();
                    }

                }

                var dto = model.Dto;
                var entity = _mapper.Map<CompanyCargo>(dto);
                entity.SellerId = dto.SellerId;
                if (!dto.Id.HasValue)
                {
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.CompanyCargoes.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.IsDefault, dto.IsDefault)
                        .SetProperty(a => a.MinBasketAmount, dto.MinBasketAmount)
                        .SetProperty(a => a.SellerId, dto.SellerId)
                        .SetProperty(a => a.Status, (dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive))
                        .SetProperty(a => a.ModifiedDate, DateTime.Now)
                        .SetProperty(a => a.ModifiedId, model.UserId));

                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Kayıt Işlemi Başarılı");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertCompanyCargo Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
