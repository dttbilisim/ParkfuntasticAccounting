using AutoMapper;
using ecommerce.Admin.Domain.Dtos.StaticPageDto;
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
namespace ecommerce.Admin.Domain.Concreate{
    public class StaticPageService : IStaticPageService{
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<StaticPage> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<StaticPageListDto> _radzenPagerService;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "staticpages";

        public StaticPageService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<StaticPageListDto> radzenPagerService, ecommerce.Admin.Domain.Services.IPermissionService permissionService){
            _context = context;
            _repository = context.GetRepository<StaticPage>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _permissionService = permissionService;
        }
        public async Task<IActionResult<Empty>> DeleteAboutUs(AuditWrapDto<StaticPageDeleteDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                //Deleted Mark with audit
                await _context.DbContext.AboutUs.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    rs.AddSuccess("Silme ??lemi Ba?ar?l?");
                    return rs;
                } else{
                    if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            } catch(Exception ex){
                _logger.LogError("DeleteAboutUs Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<StaticPageUpsertDto>> GetAboutUsById(int Id){
            var rs = new IActionResult<StaticPageUpsertDto>{Result = new()};
            try{
                var data = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == Id);
                var mapped = _mapper.Map<StaticPageUpsertDto>(data);
                if(mapped != null){
                    rs.Result = mapped;
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetAboutUsById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Paging<IQueryable<StaticPageListDto>>>> GetAboutUs(PageSetting pager){
            IActionResult<Paging<IQueryable<StaticPageListDto>>> response = new(){Result = new()};
            try{
                var datas = _repository.GetAll(predicate:f => f.Status == (int) EntityStatus.Active);
                var mapped = _mapper.Map<List<StaticPageListDto>>(datas);
                if(mapped != null){
                    if(mapped.Count > 0) response.Result.Data = mapped.AsQueryable();
                }
                if(response.Result.Data != null) response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);
                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                response.Result = result;
                return response;
            } catch(Exception ex){
                _logger.LogError("GetAboutUs Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<StaticPageListDto>>> GetAboutUs(){
            var rs = new IActionResult<List<StaticPageListDto>>{Result = new List<StaticPageListDto>()};
            try{
                var datas = await _repository.GetAllAsync(predicate:f => f.Status == 1);
                var mapped = _mapper.Map<List<StaticPageListDto>>(datas);
                if(mapped != null){
                    if(mapped.Count > 0) rs.Result = mapped;
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetAboutUss Exception " + ex.ToString());
                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpsertAboutUs(AuditWrapDto<StaticPageUpsertDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                var dto = model.Dto;
                var entity = _mapper.Map<StaticPage>(dto);
                if(!dto.Id.HasValue){
                    entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                    await _context.SaveChangesAsync();
                } else{
                    await _context.DbContext.AboutUs.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a=>a.Root,dto.Root).SetProperty(a => a.StaticPageType, dto.StaticPageType).SetProperty(a => a.Content, dto.Content).SetProperty(a => a.FileName, dto.FileName).SetProperty(a => a.FileGuid, dto.FileGuid).SetProperty(a => a.Status, (dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive)).SetProperty(a => a.ModifiedDate, DateTime.Now).SetProperty(a => a.Url, dto.Url).SetProperty(a => a.ModifiedId, model.UserId));
                }
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    await AboutUsListToPassive(entity.Id, entity.StaticPageType);
                    rs.AddSuccess("Kayıt işlemi Başarılı");
                    return rs;
                } else{
                    if(lastResult != null && lastResult.Exception != null) rs.AddError("Herhangi bir hata oluştu!!! Lütfen tekrar deneyiniz.");
                    return rs;
                }
            } catch(Exception ex){
                _logger.LogError("UpsertAboutUs Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task AboutUsListToPassive(int newAboutUsId, StaticPageType staticPageType){
            var aboutUsList = await _context.DbContext.AboutUs.Where(x => x.Id != newAboutUsId && x.StaticPageType == staticPageType).ToListAsync();
            aboutUsList.ForEach(x => {x.Status = (int) EntityStatus.Passive;});
            try{
                await _context.SaveChangesAsync();
            } catch(Exception){}
        }
    }
}
