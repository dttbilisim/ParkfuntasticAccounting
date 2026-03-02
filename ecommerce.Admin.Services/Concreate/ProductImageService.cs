using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductImageDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class ProductImageService : IProductImageService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ProductImage> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ProductImageService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<ProductImage>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<IActionResult<Empty>> UpsertProductImage(AuditWrapDto<ProductImageUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    var exists = await _context.DbContext.ProductImages.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.FileName.ToLower().Contains(dto.FileName.ToLower()));
                    if (exists == null)
                    {
                        var entity = _mapper.Map<ProductImage>(dto);
                        entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                        entity.CreatedId = model.UserId;
                        entity.CreatedDate = DateTime.Now;
                        await _repository.InsertAsync(entity);
                    }
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Görsel bulunamadı");
                        return rs;
                    }
                    var updated = _mapper.Map<ProductImage>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                }
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
            catch (Exception)
            {

                throw;
            }


            throw new NotImplementedException();
        }

        public async Task<IActionResult<Empty>> DeleteProductImage(AuditWrapDto<ProductImageDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {

                await _context.DbContext.ProductImages.Where(f => f.Id == model.Dto.Id).ExecuteDeleteAsync();


                //await _context.DbContext.ProductImages.Where(f => f.Id == model.Dto.Id)
                //      .ExecuteUpdateAsync(x =>
                //      x.SetProperty(product => product.DeletedId, model.UserId).SetProperty(x => x.Status, EntityStatus.Deleted.GetHashCode()));

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
                _logger.LogError("DeleteCompany Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ProductImageUpsertDto>> GetProductImage(int Id)
        {
            IActionResult<ProductImageUpsertDto> response = new() { Result = new() };

            try
            {
                var result = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Id == Id);



                var mapped = _mapper.Map<ProductImageUpsertDto>(result);
                if (mapped != null)
                {
                    response.Result = mapped;
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductImage Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<ProductImageListDto>>> GetProductImages(int productId)
        {
            IActionResult<List<ProductImageListDto>> response = new() { Result = new() };

            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<ProductImage>();
                    
                    var result = await repository.GetAllAsync(
                        predicate: x => x.ProductId == productId && x.Status == (int)EntityStatus.Active, 
                        disableTracking: true);
                    var mapped = _mapper.Map<List<ProductImageListDto>>(result);
                    if (mapped != null)
                    {
                        if (mapped.Count > 0)
                            response.Result = mapped.ToList();
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductImages Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<int>> GetProductImageMaxOrderNumber(int ProductId)
        {
            IActionResult<int> response = new();

            try
            {
                var images = await _repository.GetAllAsync(predicate: x => x.ProductId == ProductId && x.Status == (int)EntityStatus.Active);
                if (images.Count() > 0)
                    response.Result = images.Max(x => x.Order) + 1;
                else
                    response.Result = 1;

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductImageMaxOrderNumber Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }

        }
    }

}
