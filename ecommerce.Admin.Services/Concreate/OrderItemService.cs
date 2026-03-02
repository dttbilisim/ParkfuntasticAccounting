using AutoMapper;
using ecommerce.Admin.Domain.Dtos.OrderItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class OrderItemService : IOrderItemService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<OrderItems> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<OrderItemListDto> _radzenPagerService;
        public OrderItemService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<OrderItemListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<OrderItems>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService=radzenPagerService;
        }

        public async Task<IActionResult<Empty>> DeleteOrderItem(AuditWrapDto<OrderItemDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                await _context.DbContext.OrderItems.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteOrderItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<OrderItemUpsertDto>> GetOrderItemById(int Id)
        {
            var rs = new IActionResult<OrderItemUpsertDto> { Result = new() };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mappedCat = _mapper.Map<OrderItemUpsertDto>(data);
                if (mappedCat != null)
                {
                    rs.Result = mappedCat;
                }
                else
                    rs.AddError("Sipariş Ürünü Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOrderItemById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<OrderItemListDto>>> GetOrderItems()
        {
            IActionResult<List<OrderItemListDto>> response = new() { Result = new() };
            try
            {

                var cities = _context.DbContext.City.ToList();
                var mappedCats = _mapper.Map<List<OrderItemListDto>>(cities);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0)
                        response.Result = mappedCats.ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetOrderItems Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
