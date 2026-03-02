using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductCategory;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductCategories> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;

        public ProductCategoryService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider)
        {
            _context = context;
            _repository = context.GetRepository<ProductCategories>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
        }

        public async Task<IActionResult<Empty>> DeleteProductCategory(AuditWrapDto<ProductCategoryDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                await _context.DbContext.ProductCategories.Where(x=>x.Id==model.Dto.Id).ExecuteDeleteAsync();

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

        public async Task<IActionResult<List<ProductCategoryListDto>>> GetProductCategories(int productId)
        {
            var rs = new IActionResult<List<ProductCategoryListDto>> { Result = new() };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductCategories>();
                    
                    var entities = await repository.GetAllAsync(
                        predicate: x => x.ProductId == productId && 
                                        (isGlobalAdmin || x.BranchId == currentBranchId || x.BranchId == null || x.BranchId == 0), 
                        include: x => x.Include(p => p.Category),
                        disableTracking: true,
                        ignoreQueryFilters: true);
                    var mapped = _mapper.Map<List<ProductCategoryListDto>>(entities);
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
                _logger.LogError("GetProductCategories Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ProductCategoryUpsertDto>> GetProductCategoryById(int Id)
        {
            var rs = new IActionResult<ProductCategoryUpsertDto>
            {
                Result = new()
            };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<ProductCategoryUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductCategoryById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertProductCategory(AuditWrapDto<ProductCategoryUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductCategories>();

                    var productCategories = await repository.GetAllAsync(predicate: f => f.ProductId == dto.ProductId);

                    if (!dto.Id.HasValue)
                    {
                        if (dto.Categories != null && dto.Categories.Any())
                        {
                            foreach (var item in dto.Categories)
                            {
                                if (!productCategories.Any(x => x.CategoryId == item))
                                {
                                    var entity = new ProductCategories
                                    {
                                        ProductId = dto.ProductId,
                                        CategoryId = item,
                                        BranchId = _tenantProvider.GetCurrentBranchId()
                                    };
                                    await repository.InsertAsync(entity);
                                }
                            }
                        }
                        else if (dto.CategoryId > 0)
                        {
                             if (!productCategories.Any(x => x.CategoryId == dto.CategoryId))
                             {
                                 var entity = new ProductCategories
                                 {
                                     ProductId = dto.ProductId,
                                     CategoryId = dto.CategoryId,
                                     BranchId = _tenantProvider.GetCurrentBranchId()
                                 };
                                 await repository.InsertAsync(entity);
                             }
                        }
                        
                        await context.SaveChangesAsync();
                        
                        var lastResult = context.LastSaveChangesResult;
                        if (lastResult.IsOk)
                        {
                            rs.AddSuccess("Success");
                             return rs;
                        }
                        else
                        {
                            if (lastResult != null && lastResult.Exception != null)
                                rs.AddError(lastResult.Exception.ToString());
                            return rs;
                        }
                    }
                    else
                    {
                         // Update logic if needed, but usually only insert/delete for categories
                        await context.DbContext.ProductCategories.Where(x => x.Id == model.Dto.Id)
                            .ExecuteUpdateAsync(x => x
                            .SetProperty(c => c.ProductId, model.Dto.ProductId)
                            .SetProperty(c => c.CategoryId, model.Dto.CategoryId));
                            
                        rs.AddSuccess("Success");
                        return rs;
                    }
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
