using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductTierDto;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class ProductTierService : IProductTierService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductTier> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<ProductTierListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ProductTierService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<ProductTierListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<ProductTier>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<IActionResult<Empty>> DeleteProductTier(AuditWrapDto<ProductTierDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.ProductTiers.Where(f => f.Id == model.Dto.Id).
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
                _logger.LogError("DeleteProductTier Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ProductTierUpsertDto>> GetProductTierById(int Id)
        {
            var rs = new IActionResult<ProductTierUpsertDto>
            {
                Result = new()
            };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<ProductTierUpsertDto>(data);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductTierById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<ProductTierListDto>>>> GetProductTiers(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<ProductTierListDto>>> response = new() { Result = new() };

            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == (int)EntityStatus.Active);
                var mapped = _mapper.Map<List<ProductTierListDto>>(datas);
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
                _logger.LogError("GetProductTiers Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<ProductTierListDto>>> GetProductTiers()
        {
            var rs = new IActionResult<List<ProductTierListDto>>
            {
                Result = new List<ProductTierListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == 1);
                var mapped = _mapper.Map<List<ProductTierListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductTiers Exception " + ex.ToString());

                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<ProductTierListDto>>> GetProductTiers(int productId)
        {
            var rs = new IActionResult<List<ProductTierListDto>>
            {
                Result = new List<ProductTierListDto>()
            };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductTier>();
                    
                    var datas = await repository.GetAllAsync(
                        predicate: f => f.Status == 1 && f.ProductId == productId, 
                        include: x => x.Include(f => f.Tier),
                        disableTracking: true);
                    var mapped = _mapper.Map<List<ProductTierListDto>>(datas);
                    if (mapped != null)
                    {
                        if (mapped.Count > 0)
                            rs.Result = mapped;
                    }
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductTiers Exception " + ex.ToString());

                rs.AddError("Liste Alınamadı");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertProductTier(AuditWrapDto<ProductTierUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<ProductTier>(dto);
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
                    await _context.DbContext.ProductTiers.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.ProductId, dto.ProductId)
                        .SetProperty(a => a.TierId, dto.TierId)
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
                _logger.LogError("UpsertProductTier Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
