using AutoMapper;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using Microsoft.Extensions.Logging;
using ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Admin.Domain.Concreate
{
    public class ProductActiveArticleService : IProductActiveArticleService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductActiveArticleItem> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<ProductActiveArticleListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ProductActiveArticleService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<ProductActiveArticleListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<ProductActiveArticleItem>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
        } 


        public async Task<IActionResult<Empty>> DeleteProductActiveArticle(AuditWrapDto<ProductActiveArticleDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                await _context.DbContext.ProductActiveArticleItems.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
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
                _logger.LogError("DeleteProductActiveArticle Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ProductActiveArticleUpsertDto>> GetProductActiveArticleById(int Id)
        {
            var rs = new IActionResult<ProductActiveArticleUpsertDto>
            {
                Result = new()
            };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<ProductActiveArticleUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductActiveArticleById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<ProductActiveArticleListDto>>> GetProductActiveArticles(int productId)
        {
            var rs = new IActionResult<List<ProductActiveArticleListDto>> { Result = new() };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductActiveArticleItem>();
                    
                    var entities = await repository.GetAllAsync(
                        predicate: x => x.ProductId == productId && x.Status == (int)EntityStatus.Active, 
                        include: x => x.Include(p => p.ActiveArticle).Include(p => p.ScaleUnit),
                        disableTracking: true);
                    var mapped = _mapper.Map<List<ProductActiveArticleListDto>>(entities);
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
                _logger.LogError("GetProductActiveArticles Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        } 

        public async Task<IActionResult<Empty>> UpsertProductActiveArticle(AuditWrapDto<ProductActiveArticleUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<ProductActiveArticleItem>(dto);
                    entity.Status = (int)EntityStatus.Active;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    entity.ProductId = dto.ProductId.Value;
                    await _repository.InsertAsync(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.ProductActiveArticleItems.Where(x => x.Id == model.Dto.Id)
                        .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.ProductId, model.Dto.ProductId)
                        .SetProperty(c => c.ActiveArticleId, model.Dto.ActiveArticleId)
                        .SetProperty(c => c.ScaleUnitId, model.Dto.ScaleUnitId)
                        .SetProperty(c => c.Amount, model.Dto.Amount)
                        .SetProperty(c => c.Status, (int)EntityStatus.Active)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));
                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
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
                _logger.LogError("UpsertScaleUnits Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
