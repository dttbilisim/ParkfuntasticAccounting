using AutoMapper;
using ecommerce.Admin.Domain.Dtos.Scheduler;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate;
public class EducationCalendarService : IEducationCalendarService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<EducationCalendar> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly IRadzenPagerService<EducationCalendarListDto> _radzenPagerService;
    public EducationCalendarService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, CurrentUser currentUser, IRadzenPagerService<EducationCalendarListDto> radzenPagerService){
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _radzenPagerService = radzenPagerService;
        _repository = context.GetRepository<EducationCalendar>();
    }
    public async Task<IActionResult<Paging<List<EducationCalendarListDto>>>> GetAll(PageSetting pager){
        var response = OperationResult.CreateResult<Paging<List<EducationCalendarListDto>>>();
        try{
            response.Result = await _repository.GetAll(true).ToPagedResultAsync<EducationCalendarListDto>(pager, _mapper);
        } catch(Exception e){
            _logger.LogError("EducationCalendarListDto Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<List<EducationCalendarListDto>>> Get(){
        var response = OperationResult.CreateResult<List<EducationCalendarListDto>>();
        try{
            var editorialContents = await _repository.GetAllAsync(predicate:s => s.Status != (int) EntityStatus.Deleted);
            response.Result = _mapper.Map<List<EducationCalendarListDto>>(editorialContents);
        } catch(Exception e){
            _logger.LogError("EducationCalendarListDto Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<Empty>> Upsert(AuditWrapDto<EducationCalendarUpsertDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var dto = model.Dto;
            var entity = _mapper.Map<EducationCalendar>(dto);
            if(!dto.Id.HasValue){
                entity.Status = (int) dto.Status;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                entity.Color = "#212121";
                await _repository.InsertAsync(entity);
            } else{
                entity = await _context.DbContext.EducationCalendars.FirstOrDefaultAsync(x => x.Id == dto.Id);
                entity.Name = dto.Name;
                entity.Status = (int) dto.Status;
                entity.CompanyId = dto.CompanyId;
                entity.Description = dto.Description;
                entity.StartDate = dto.StartDate;
                entity.EndDate = dto.EndDate;
                entity.Color = dto.Color;
                entity.StartDate = dto.StartDate;
                entity.EndDate = dto.EndDate;
                entity.Url = dto.Url;
                entity.PharmacyTypeId = dto.PharmacyTypeId;
                entity.UserTypeId = dto.UserTypeId;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                entity.Color = "#212121";
                // entity takipli; Update çağırmaya gerek yok
            }
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if(lastResult.IsOk){
                if(!dto.Id.HasValue){
                    var users = await _context.DbContext.Company.Where(x => x.UserType == dto.UserTypeId && x.Status == 1).ToListAsync();
                    if(users.Count > 0){
                        foreach(var user in users){
                            Notification notification = new();
                            notification.CompanyId = user.Id;
                            notification.CreatedDate = DateTime.Now;
                            notification.CreatedId = (int) dto.UserTypeId;
                            notification.Status = 1;
                            notification.IsRead = false;
                            notification.ProcessCode = "calendar";
                            if(entity.StartDate == entity.EndDate){
                                notification.Description = "Parpazar tarafından " + entity.Name + " isimli " + entity.StartDate.ToShortDateString() + "tarihli takvimize kayıt eklendi";
                            } else{
                                notification.Description = "Parpazar tarafından " + entity.Name + " isimli " + entity.StartDate.ToShortDateString() + "-" + entity.EndDate.ToShortDateString() + " tarihler arasında takvimize kayıt eklendi";
                            }

                            await _context.DbContext.Notifications.AddAsync(notification);
                        }
                        await _context.SaveChangesAsync();
                    }
                }
               
                rs.AddSuccess("Successfull");
                return rs;
            } else{
                if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                return rs;
            }
        } catch(Exception ex){
            _logger.LogError("Upsert Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> Delete(AuditWrapDto<EducationCalendarDeleteDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
           var data= await _context.DbContext.EducationCalendars.FirstOrDefaultAsync(f => f.Id == model.Dto.Id);
           _context.DbContext.EducationCalendars.Remove(data);
         
            
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
            _logger.LogError("DeleteBanner Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<EducationCalendarUpsertDto>> GetById(int Id){
        var response = OperationResult.CreateResult<EducationCalendarUpsertDto>();
        try{
            var data = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == Id);
            if(data == null){
                response.AddError("İçerik bulunamadı");
                return response;
            }
            response.Result = _mapper.Map<EducationCalendarUpsertDto>(data);
        } catch(Exception e){
            _logger.LogError("GetById Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
}
