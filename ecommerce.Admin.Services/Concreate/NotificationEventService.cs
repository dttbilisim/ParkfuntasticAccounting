using AutoMapper;
using ecommerce.Admin.Domain.Dtos.NotificationEventDto;
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
    public class NotificationEventService : INotificationEventService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<NotificationEvent> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<NotificationEventListDto> _radzenPagerService;

        public NotificationEventService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<NotificationEventListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<NotificationEvent>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }

        public async Task<IActionResult<Empty>> DeleteNotificationEvent(AuditWrapDto<NotificationEventDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.NotificationEvents.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Silme İşlemi Başarılı");
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
                _logger.LogError("DeleteNotificationEvent Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<NotificationEventUpsertDto>> GetNotificationEventById(int Id)
        {
            var rs = new IActionResult<NotificationEventUpsertDto>
            {
                Result = new()
            };
            try
            {
                var events = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<NotificationEventUpsertDto>(events);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteNotificationEvent Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<NotificationEventListDto>>> GetNotificationEvents()
        {
            var rs = new IActionResult<List<NotificationEventListDto>>
            {
                Result = new List<NotificationEventListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == 1);
                var mapped = _mapper.Map<List<NotificationEventListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNotificationEvents Exception " + ex.ToString());

                rs.AddError("Liste Alınamadı");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<NotificationEventListDto>>>> GetNotificationEvents(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<NotificationEventListDto>>> response = new() { Result = new() };

            try
            {
                var categories = await _repository.GetAllAsync(predicate: f => f.Status == 1);
                var mapped = _mapper.Map<List<NotificationEventListDto>>(categories);
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
                _logger.LogError("GetNotificationEvents Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertNotificationEvent(AuditWrapDto<NotificationEventUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<NotificationEvent>(dto);
                if (!dto.Id.HasValue)
                {
                    entity.Status = 1;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.NotificationEvents.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.NotificationType, dto.NotificationType)
                        .SetProperty(a => a.Template, dto.Template)
                        .SetProperty(a => a.Value, dto.Value)
                        .SetProperty(a => a.NotificationStatus, dto.NotificationStatus)
                        .SetProperty(a => a.Status, (dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive))
                        .SetProperty(a => a.ModifiedDate, DateTime.Now)
                        .SetProperty(a => a.ModifiedId, model.UserId));

                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Gönderilecek Email Listesine Eklendi");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError("Email gönderilecekler listesine eklenemedi!!!");
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertNotificationEvent Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

     
    }
}
