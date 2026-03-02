using AutoMapper;
using ecommerce.Admin.Domain.Dtos.EmailDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate;
public class EmailTemplateService : IEmailTemplateService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<EmailTemplates> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<EmailTemplatesDto> _radzenPagerService;
    private readonly ITenantProvider _tenantProvider;

    public EmailTemplateService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<EmailTemplatesDto> radzenPagerService, ITenantProvider tenantProvider){
        _context = context;
        _repository = context.GetRepository<EmailTemplates>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult<Paging<IQueryable<EmailTemplatesDto>>>> GetAllPaging(PageSetting pager){
        IActionResult<Paging<IQueryable<EmailTemplatesDto>>> response = new(){Result = new()};
        try{
            var branchId = _tenantProvider.GetCurrentBranchId();
            // Mevcut şube + BranchId null (eski/ortak) kayıtları göster; şube seçilmemişse (0) tüm aktif kayıtlar
            var datas = _repository.GetAll(predicate:f => f.Status == (int) EntityStatus.Active && (branchId == 0 || f.BranchId == branchId || f.BranchId == null));
            var mapped = _mapper.Map<List<EmailTemplatesDto>>(datas);
            if(mapped != null){
                if(mapped.Count > 0) response.Result.Data = mapped.AsQueryable();
            }
            if(response.Result.Data != null) response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);
            var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            response.Result = result;
            return response;
        } catch(Exception ex){
            _logger.LogError("GetAll Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }
    public async Task<IActionResult<List<EmailTemplatesDto>>> Get(){
        var rs = new IActionResult<List<EmailTemplatesDto>>{Result = new List<EmailTemplatesDto>()};
        try{
            var branchId = _tenantProvider.GetCurrentBranchId();
            var datas = await _repository.GetAllAsync(predicate:f => f.Status == 1 && (branchId == 0 || f.BranchId == branchId || f.BranchId == null));
            var mapped = _mapper.Map<List<EmailTemplatesDto>>(datas);
            if(mapped != null){
                if(mapped.Count > 0) rs.Result = mapped;
            }
            return rs;
        } catch(Exception ex){
            _logger.LogError("Get Exception " + ex.ToString());
            rs.AddError("Liste Al?namad?");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> Upsert(AuditWrapDto<EmailTemplatesDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var branchId = _tenantProvider.GetCurrentBranchId();
            var dto = model.Dto;
            var entity = _mapper.Map<EmailTemplates>(dto);
            if (dto.Id == null || dto.Id == 0){
                var checkTemplate = await _context.DbContext.EmailTemplates.FirstOrDefaultAsync(x => x.EmailTemplateType == dto.EmailTemplateType && x.Status == 1 && x.BranchId == branchId);
                if(checkTemplate != null){
                    rs.AddError("Bu email şablonu bu şubede zaten eklenmiştir. Farklı bir şablon seçiniz.");
                    return rs;
                }
                entity.BranchId = branchId;
                entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _repository.InsertAsync(entity);
                await _context.SaveChangesAsync();
            } else{
                await _context.DbContext.EmailTemplates.Where(f => f.Id == model.Dto.Id && (f.BranchId == branchId || f.BranchId == null)).ExecuteUpdateAsync(s => s.SetProperty(a => a.Name, dto.Name).SetProperty(a => a.Description, dto.Description).SetProperty(a => a.Status, (dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive)).SetProperty(a => a.EmailTemplateType, dto.EmailTemplateType).SetProperty(a => a.ModifiedDate, DateTime.Now).SetProperty(a => a.ModifiedId, model.UserId));
            }
            var lastResult = _context.LastSaveChangesResult;
            if(lastResult.IsOk){
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
    public async Task<IActionResult<Empty>> Delete(AuditWrapDto<EmailTemplatesDto> model){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            var branchId = _tenantProvider.GetCurrentBranchId();
            await _context.DbContext.EmailTemplates.Where(f => f.Id == model.Dto.Id && (f.BranchId == branchId || f.BranchId == null)).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
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
            _logger.LogError("Delete Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
    public async Task<IActionResult<EmailTemplatesDto>> GetById(int Id){
        var rs = new IActionResult<EmailTemplatesDto>{Result = new()};
        try{
            var branchId = _tenantProvider.GetCurrentBranchId();
            var data = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == Id && (f.BranchId == branchId || f.BranchId == null));
            var mapped = _mapper.Map<EmailTemplatesDto>(data);
            if(mapped != null){
                rs.Result = mapped;
            }
            return rs;
        } catch(Exception ex){
            _logger.LogError("GetById Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}
