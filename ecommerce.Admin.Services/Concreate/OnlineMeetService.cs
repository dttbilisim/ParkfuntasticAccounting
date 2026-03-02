using AutoMapper;
using ecommerce.Admin.Domain.Dtos;
using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.ZoomDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate;
public class OnlineMeetService : IOnlineMeetService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<OnlineMeetCalender> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<OnlineMeetDto> _radzenPagerService;
    private readonly IZoomService _zoomService;
    private readonly IEducationService _educationService;
    private readonly IEmailService _emailService;
    public OnlineMeetService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<OnlineMeetDto> radzenPagerService, IZoomService zoomService, IEducationService educationService, IEmailService emailService){
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _repository = context.GetRepository<OnlineMeetCalender>();
        _radzenPagerService = radzenPagerService;
        _zoomService = zoomService;
        _educationService = educationService;
        _emailService = emailService;
    }
    public async Task<IActionResult<Paging<IQueryable<OnlineMeetDto>>>> GetOnlineMeet(PageSetting pager){
        IActionResult<Paging<IQueryable<OnlineMeetDto>>> response = new(){Result = new()};
        try{
            var datas = await _repository.GetAllAsync(predicate:f => f.Status != (int) EntityStatus.Deleted, include:x => x.Include(x => x.Seller).Include(x => x.Seller).Include(x => x.OnlineMeetCalendarPharmacies));
            var mapped = _mapper.Map<List<OnlineMeetDto>>(datas);
            if(mapped is{Count: > 0}) response.Result.Data = mapped.AsQueryable();
            if(response.Result.Data != null) response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);
            var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            response.Result = result;
            return response;
        } catch(Exception ex){
            _logger.LogError("GetOnlineMeet Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }
    public async Task<IActionResult<List<OnlineMeetDto>>> GetOnlineMeet(){
        var rs = new IActionResult<List<OnlineMeetDto>>{Result = new List<OnlineMeetDto>()};
        try{
            var datas = await _repository.GetAllAsync(predicate:f => f.Status == 1);
            var mapped = _mapper.Map<List<OnlineMeetDto>>(datas);
            if(mapped != null){
                if(mapped.Count > 0) rs.Result = mapped;
            }
            return rs;
        } catch(Exception ex){
            _logger.LogError("GetOnlineMeet Exception " + ex.ToString());
            rs.AddError("Liste Alinamadi");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<int>> UpsertMeet(AuditWrapDto<OnlineMeetUpsertDto> model){
        var rs = new IActionResult<int>();
        try{
            var dto = model.Dto;
            var seller = await _context.DbContext.Company.FirstOrDefaultAsync(x => x.Id == model.Dto.SellerId);
            var sellerName = seller.BankAccountName ?? seller.AccountName;
            model.Dto.SellerEmail ??= seller.EmailAddress;
            var MeetSubject = $"{model.Dto.Subject} -{model.Dto.MeetDate.ToString("HH:mm")} ({model.Dto.Duration} dakika) ";
            var meetDesc = "İkili Görüşme Takviminize Yeni Bir Görüşme Eklendi.";
            var meetUpdateDesc = "İkili Görüşme Takviminizdeki Bir Görüşme Güncellendi.";
            var DescriptionTextSeller = $"Merhaba Sayın : {model.Dto.SellerName}  {model.Dto.MeetDate.ToString("dd MMMM yyyy HH:mm")} tarihi ve saatinde gerçekleşecek {model.Dto.Duration} dakikalık görüşmeniz mevcuttur,Toplantı parolanız: {model.Dto.Password}";
            var entity = _mapper.Map<OnlineMeetCalender>(dto);
            if(!dto.Id.HasValue){
                var meetCreate = await _zoomService.CreateMeeting(new ZoomCreateRequestDto{OnlineMeetUpsert = model.Dto});
                if(meetCreate.Result.Id != 0){

                    var users = await _zoomService.GetMeetingRegistrants(meetCreate.Result.Id);
                
                    // marka takvimi 
                    var educationCalendarSeller = new Appointment{
                        Status = 1,
                        Name = MeetSubject,
                        CompanyId = model.Dto.SellerId,
                        StartDate = Convert.ToDateTime(model.Dto.MeetDate.ToShortDateString()),
                        EndDate = Convert.ToDateTime(model.Dto.MeetDate.AddMinutes(model.Dto.Duration).ToShortDateString()),
                        Color = "#212121",
                        Description = DescriptionTextSeller,
                        CreatedDate = DateTime.Now,
                        CreatedId = model.UserId,
                        Url = users.Registrants.FirstOrDefault(x => x.Email == model.Dto.SellerEmail).JoinUrl,
                        IsZoom = true,
                        SubjectType = 5,
                        IsCompany = true,
                        MeetId = meetCreate.Result.Id
                    };
                    var notificationSeller = new Notification{
                        Description = DescriptionTextSeller,
                        CompanyId = model.Dto.SellerId,
                        Status = 1,
                        CreatedDate = DateTime.Now,
                        CreatedId = model.UserId,
                        ProcessCode = "calendar",
                        IsRead = false
                    };
                    await _context.DbContext.Appointments.AddAsync(educationCalendarSeller);
                    await _context.DbContext.Notifications.AddAsync(notificationSeller);
                    await _context.SaveChangesAsync();
                    entity.Status = (int) dto.Status;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    entity.Password = model.Dto.Password;
                    entity.MeetId = meetCreate.Result.Id;
                    entity.MeetLink = meetCreate.Result.JoinUrl;
                    await _repository.InsertAsync(entity);
                    await _context.SaveChangesAsync();
                    foreach(var meetUser in model.Dto.OnlineMeetCalendarPharmacies){
                        meetUser.OnlineMeetId = entity.Id;
                        var existingPharmacy = await _context.DbContext.OnlineMeetCalendarPharmacies.FirstOrDefaultAsync(p => p.OnlineMeetId == meetUser.OnlineMeetId && p.CompanyId == meetUser.CompanyId);
                        if(existingPharmacy != null){
                            existingPharmacy.CompanyName = meetUser.CompanyName;
                            existingPharmacy.Name = meetUser.Name;
                            existingPharmacy.SurName = meetUser.SurName;
                            existingPharmacy.Email = meetUser.Email;
                            existingPharmacy.IsApproved = meetUser.IsApproved;
                            _context.DbContext.OnlineMeetCalendarPharmacies.Update(existingPharmacy);
                        } else{
                            await _context.DbContext.OnlineMeetCalendarPharmacies.AddAsync(meetUser);
                        }
                    }
            
                    await _context.SaveChangesAsync();
                    var DescriptionTextCompany = $"Merhaba Sayın Üyemiz: {sellerName} Firması tarafından {model.Dto.MeetDate.ToString("dd MMMM yyyy HH:mm")} tarihi ve saatinde gerçekleşecek {model.Dto.Duration} dakikalık görüşmenize aşağıdaki linkten ulaşabilirsiniz,Toplantı parolanız: {model.Dto.Password}, Görüşmenizin aktif olması icin aşağıdaki onay kutusunu işaretleyip kaydet butonuna basmanız gerekmektedir.";
                    await _emailService.ZoomMeetCreate(model.Dto.SellerName, model.Dto.Subject, meetDesc, model.Dto.MeetDate, model.Dto.Duration, entity.SellerEmail);
            
                    foreach(var company in model.Dto.OnlineMeetCalendarPharmacies){
                        //eczanelerin takvimi ve bildirimi

                        //takvime ekliyoruz
                        var educationCalendarCompany = new Appointment{
                            Status = 1,
                            Name = MeetSubject,
                            CompanyId = company.CompanyId,
                            StartDate = Convert.ToDateTime(model.Dto.MeetDate.ToShortDateString()),
                            EndDate = Convert.ToDateTime(model.Dto.MeetDate.AddMinutes(model.Dto.Duration).ToShortDateString()),
                            Color = "#212121",
                            Description = DescriptionTextCompany,
                            CreatedDate = DateTime.Now,
                            CreatedId = model.UserId,
                            Url = users.Registrants.FirstOrDefault(x => x.Email == company.Email).JoinUrl,
                            IsZoom = true,
                            SubjectType = 5,
                            MeetId = meetCreate.Result.Id
                        };
                        //bildirim gonderiyoruz.
                        var notificationCompany = new Notification{
                            Description = DescriptionTextCompany,
                            CompanyId = company.CompanyId,
                            Status = 1,
                            CreatedDate = DateTime.Now,
                            CreatedId = model.UserId,
                            ProcessCode = "calendar",
                            IsRead = false
                        };
                        //bildirim gonderiyoruz.
                        await _context.DbContext.Appointments.AddAsync(educationCalendarCompany);
                        await _context.DbContext.Notifications.AddAsync(notificationCompany);
                        await _emailService.ZoomMeetCreate(company.Name + " " + company.SurName, model.Dto.Subject, meetDesc, model.Dto.MeetDate, model.Dto.Duration, company.Email);
                        await _context.SaveChangesAsync();
                    }
                }
                rs.Result = entity.Id;
            } else{
                await _zoomService.CancelMeetingAsync(model.Dto.MeetId.Value);
                var meetCreate = await _zoomService.CreateMeeting(new ZoomCreateRequestDto{OnlineMeetUpsert = model.Dto});
                if(meetCreate.Result.JoinUrl != null){
                    var users = await _zoomService.GetMeetingRegistrants(meetCreate.Result.Id);
                    // toplantiyi guncelledik.
                    await _context.DbContext.OnlineMeetCalenders.Where(f => f.MeetId == model.Dto.MeetId).ExecuteUpdateAsync(s => s.SetProperty(a => a.SellerId, dto.SellerId).SetProperty(a => a.SellerName, dto.SellerName).SetProperty(a => a.MeetDate, dto.MeetDate).SetProperty(a => a.MeetLink, meetCreate.Result.JoinUrl).SetProperty(a => a.Description, dto.Description).SetProperty(a => a.Subject, dto.Subject).SetProperty(a => a.Status, (int) dto.Status).SetProperty(a => a.Duration, dto.Duration).SetProperty(a => a.ModifiedId, model.UserId).SetProperty(a => a.SellerEmail, model.Dto.SellerEmail).SetProperty(a => a.ModifiedDate, DateTime.Now).SetProperty(a => a.Password, dto.Password).SetProperty(a => a.MeetId, meetCreate.Result.Id));

                    //katilimcilari guncelleme ekle
                    foreach(var meetUser in model.Dto.OnlineMeetCalendarPharmacies){
                        var checkCompany = _context.DbContext.OnlineMeetCalendarPharmacies.FirstOrDefault(x => x.CompanyId == meetUser.CompanyId);
                        if(checkCompany != null){
                            await _context.DbContext.OnlineMeetCalendarPharmacies.Where(x => x.OnlineMeetId == model.Dto.MeetId && x.CompanyId == meetUser.CompanyId).ExecuteUpdateAsync(x => x.SetProperty(x => x.CompanyName, meetUser.CompanyName).SetProperty(x => x.Name, meetUser.Name).SetProperty(x => x.SurName, meetUser.SurName).SetProperty(x => x.Email, meetUser.Email));
                        } else{
                            meetUser.OnlineMeetId = model.Dto.Id.Value;
                            await _context.DbContext.OnlineMeetCalendarPharmacies.AddAsync(meetUser);
                        }
                    }
                    //marka takvini guncelle
                    await _context.DbContext.Appointments.Where(x => x.MeetId == model.Dto.MeetId && x.CompanyId == model.Dto.SellerId).ExecuteUpdateAsync(x => x.SetProperty(x => x.Name, model.Dto.Subject).SetProperty(x => x.Description, DescriptionTextSeller).SetProperty(x => x.StartDate, Convert.ToDateTime(model.Dto.MeetDate.ToShortDateString())).SetProperty(x => x.EndDate, Convert.ToDateTime(model.Dto.MeetDate.AddMinutes(model.Dto.Duration).ToShortDateString()))
                        .SetProperty(x => x.Url, users.Registrants.FirstOrDefault(x => x.Email == model.Dto.SellerEmail).JoinUrl).SetProperty(x => x.ModifiedDate, DateTime.Now).SetProperty(x => x.ModifiedId, model.UserId));

                    //markaya bildirim at
                    var notificationSeller = new Notification{
                        Description = DescriptionTextSeller,
                        CompanyId = model.Dto.SellerId,
                        Status = 1,
                        CreatedDate = DateTime.Now,
                        CreatedId = model.UserId,
                        ProcessCode = "calendar",
                        IsRead = false
                    };
                    await _context.DbContext.Notifications.AddAsync(notificationSeller);
                    await _context.SaveChangesAsync();

                    // burasi katilimcilar
                    await _emailService.ZoomMeetCreate(model.Dto.SellerName, model.Dto.Subject, meetDesc, model.Dto.MeetDate, model.Dto.Duration, entity.SellerEmail);
                    var DescriptionTextCompany = $"Merhaba Sayın Üyemiz: {sellerName} Firması tarafından {model.Dto.MeetDate.ToString("dd MMMM yyyy HH:mm")} tarihi ve saatinde gerçekleşecek {model.Dto.Duration} dakikalık görüşmenize aşağıdaki linkten ulaşabilirsiniz,Toplantı parolanız: {model.Dto.Password}, Görüşmenizin aktif olması icin aşağıdaki onay kutusunu işaretleyip kaydet butonuna basmanız gerekmektedir.";
                    foreach(var company in model.Dto.OnlineMeetCalendarPharmacies){
                        await _context.DbContext.Appointments.Where(x => x.Id == model.Dto.CalenderId && x.CompanyId == company.CompanyId).ExecuteUpdateAsync(x => x.SetProperty(x => x.Name, model.Dto.Subject).SetProperty(x => x.Description, DescriptionTextCompany).SetProperty(x => x.StartDate, Convert.ToDateTime(model.Dto.MeetDate.ToShortDateString())).SetProperty(x => x.EndDate, Convert.ToDateTime(model.Dto.MeetDate.AddMinutes(model.Dto.Duration).ToShortDateString()))
                            .SetProperty(x => x.Url, users.Registrants.FirstOrDefault(x=>x.Email==company.Email).JoinUrl).SetProperty(x => x.ModifiedDate, DateTime.Now).SetProperty(x => x.ModifiedId, model.UserId));
                        var notificationCompany = new Notification{
                            Description = DescriptionTextCompany,
                            CompanyId = company.CompanyId,
                            Status = 1,
                            CreatedDate = DateTime.Now,
                            CreatedId = model.UserId,
                            ProcessCode = "calendar",
                            IsRead = false
                        };
                        await _context.DbContext.Notifications.AddAsync(notificationCompany);
                        await _context.SaveChangesAsync();
                        await _emailService.ZoomMeetCreate(company.CompanyName, model.Dto.Subject, meetDesc, model.Dto.MeetDate, model.Dto.Duration, company.Email);
                    }
                }
            }
            var isSuccess = _context.LastSaveChangesResult?.IsOk ?? false;
            if(isSuccess)
                rs.AddSuccess("Kayıt İşlemi Başarılı");
            else{
                var errorMessage = _context.LastSaveChangesResult?.Exception?.ToString();
                rs.AddError(errorMessage);
            }
            return rs;
        } catch(Exception ex){
            _logger.LogError("UpsertMeet Exception " + ex);
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> DeleteMeet(AuditWrapDto<OnlineMeetDeleteDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var meetdeleteDesc = "İkili Görüşme Takviminizdeki Bir Görüşme İptal Edildi";
            await _context.DbContext.OnlineMeetCalendarPharmacies.Where(x => x.OnlineMeetId == model.Dto.MeetId).ExecuteDeleteAsync();
            await _context.DbContext.OnlineMeetCalenders.Where(x => x.Id == model.Dto.Id).ExecuteDeleteAsync();
            await _context.DbContext.Appointments.Where(x => x.MeetId == model.Dto.MeetId && x.CompanyId == model.Dto.SellerId).ExecuteDeleteAsync();
            foreach(var compnay in model.Dto.OnlineMeetCalendarPharmacies){
                await _context.DbContext.Appointments.Where(x => x.MeetId == model.Dto.MeetId && x.CompanyId == compnay.CompanyId).ExecuteDeleteAsync();
                await _emailService.ZoomMeetCreate(compnay.Name+" "+compnay.SurName, model.Dto.Subject, meetdeleteDesc, model.Dto.MeetDate, model.Dto.Duration, compnay.Email);
            }
            await _context.SaveChangesAsync();
            await _emailService.ZoomMeetCreate(model.Dto.SellerName, model.Dto.Subject, meetdeleteDesc, model.Dto.MeetDate, model.Dto.Duration, model.Dto.SellerEmail);
            var lastResult = _context.LastSaveChangesResult;
            if(lastResult.IsOk){
                rs.AddSuccess("Silme Islemi Basarili");
                return rs;
            } else{
                if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                return rs;
            }
        } catch(Exception ex){
            _logger.LogError("DeleteMeet Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<OnlineMeetUpsertDto>> GetMeetById(int Id){
        var rs = new IActionResult<OnlineMeetUpsertDto>{Result = new()};
        try{
            var data = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == Id, include:x => x.Include(x => x.OnlineMeetCalendarPharmacies));
            var mapped = _mapper.Map<OnlineMeetUpsertDto>(data);
            if(mapped != null){
                rs.Result = mapped;
            }
            return rs;
        } catch(Exception ex){
            _logger.LogError("GetMeetById Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public DateTime ConvertToUtc(DateTime localDateTime, string timeZoneId = "Europe/Istanbul"){
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
        return utcDateTime;
    }
}
