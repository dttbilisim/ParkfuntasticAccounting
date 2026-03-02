using AutoMapper;
using ecommerce.Admin.Domain.Dtos.NotificationTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class NotificationTypeService : INotificationTypeService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<NotificationType> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<NotificationTypeListDto> _radzenPagerService;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "notificationtypes";

        public NotificationTypeService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<NotificationTypeListDto> radzenPagerService, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<NotificationType>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Empty>> DeleteNotificationType(AuditWrapDto<NotificationTypeDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.NotificationTypes.Where(f => f.Id == model.Dto.Id).ExecuteDeleteAsync();

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
                _logger.LogError("DeleteNotificationType Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<NotificationTypeUpsertDto>> GetNotificationTypeById(int Id)
        {
            var rs = new IActionResult<NotificationTypeUpsertDto>
            {
                Result = new()
            };
            try
            {
                var events = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<NotificationTypeUpsertDto>(events);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteNotificationType Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<NotificationTypeListDto>>>> GetNotificationTypes(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<NotificationTypeListDto>>> response = new() { Result = new() };

            try
            {
                var categories = await _repository.GetAllAsync(predicate: null);
                var mapped = _mapper.Map<List<NotificationTypeListDto>>(categories);
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
                _logger.LogError("GetNotificationTypes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<NotificationTypeListDto>>> GetNotificationTypes()
        {
            var rs = new IActionResult<List<NotificationTypeListDto>>
            {
                Result = new List<NotificationTypeListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: null);
                var mapped = _mapper.Map<List<NotificationTypeListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetNotificationTypes Exception " + ex.ToString());

                rs.AddError("Liste Alınamadı");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertNotificationType(AuditWrapDto<NotificationTypeUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<NotificationType>(dto);
                if (!dto.Id.HasValue)
                {
                    await _repository.InsertAsync(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.NotificationTypes.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.NotificationTypeList, dto.NotificationTypeList)
                        .SetProperty(a => a.Name, dto.Name)
                        .SetProperty(a => a.Value, dto.Value));

                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Bildirim Listesine Eklendi");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError("Bildirim listesine eklenemedi!!!");
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertNotificationType Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
