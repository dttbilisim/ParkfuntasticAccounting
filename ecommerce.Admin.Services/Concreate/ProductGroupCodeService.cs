using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate;
public class ProductGroupCodeService : IProductGroupCodeService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<ProductGroupCode> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public ProductGroupCodeService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory){
        _context = context;
        _repository = context.GetRepository<ProductGroupCode>();
        _mapper = mapper;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }
    public async Task<IActionResult<List<ProductGroupCodeListDto>>> GetProductGroupCodes(int productId){

        IActionResult<List<ProductGroupCodeListDto>> response = new(){Result = new()};
        try{
            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repository = context.GetRepository<ProductGroupCode>();
                
                var productsGroupCodes = await repository.GetAllAsync(
                    predicate: x => x.ProductId == productId, 
                    disableTracking: true);
                var mappedEntites = _mapper.Map<List<ProductGroupCodeListDto>>(productsGroupCodes);
                if(mappedEntites != null){
                    if(mappedEntites.Count > 0) response.Result = mappedEntites;
                }
            }

            return response;
        } catch(Exception ex){
            _logger.LogError("GetProductGroupCodes Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }
    public async Task<IActionResult<Empty>> DeleteProductGroupCode(AuditWrapDto<ProductGroupCodeDeleteDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var productGroupCode= await _context.DbContext.ProductGroupCodes.FirstOrDefaultAsync(f => f.Id == model.Dto.Id);
             _context.DbContext.ProductGroupCodes.Remove(productGroupCode);
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if(lastResult.IsOk){
                rs.AddSuccess("Successfull");
                return rs;
            } else{
                if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                return rs;
            }
        } catch(Exception ex){
            _logger.LogError("DeleteProductGroupCode Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> UpsertProductGroupCde(AuditWrapDto<ProductGroupCodeUpsertDto> model){

        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var dto = model.Dto;
            if(!dto.Id.HasValue){
                var entity = _mapper.Map<ProductGroupCode>(dto);
                entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _repository.InsertAsync(entity);

                await _context.SaveChangesAsync();
            } else{
                await _context.DbContext.ProductGroupCodes.Where(x => x.Id == model.Dto.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.OemCode, model.Dto.OemCode).SetProperty(c => c.Status, dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive).SetProperty(c => c.ModifiedId, model.UserId).SetProperty(c => c.ModifiedDate, DateTime.Now));
            }
            var lastResult = _context.LastSaveChangesResult;
            if(lastResult.IsOk){
                rs.AddSuccess("Successfull");
                return rs;
            } else{
                if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                return rs;
            }
           
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertProductGroupCde Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
   
}
    

