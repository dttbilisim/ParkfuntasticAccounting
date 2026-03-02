using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Helpers;
namespace ecommerce.Admin.Domain.Concreate;
public class SurveyService : ISurveyService{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<Survey> _surveyRepository;
    private readonly IRepository<SurveyAnswer> _surveyAnswerRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "Surveys";

    public SurveyService(
        IUnitOfWork<ApplicationDbContext> context, 
        IMapper mapper, 
        ILogger logger, 
        CurrentUser currentUser, 
        ITenantProvider tenantProvider, 
        IHttpContextAccessor httpContextAccessor,
        ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _surveyRepository = context.GetRepository<Survey>();
        _surveyAnswerRepository = context.GetRepository<SurveyAnswer>();
        _tenantProvider = tenantProvider;
        _httpContextAccessor = httpContextAccessor;
        _roleFilter = roleFilter;
        _permissionService = permissionService;
    }

    private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
    private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
    private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
    private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);
    public async Task<IActionResult<Paging<List<SurveyListDto>>>> GetSurveys(PageSetting pager){
        var response = OperationResult.CreateResult<Paging<List<SurveyListDto>>>();
        try{
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _surveyRepository.GetAll(true)
                .Where(s => s.Status != (int)EntityStatus.Deleted);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            response.Result = await query.ToPagedResultAsync<SurveyListDto>(pager, _mapper);
        } catch(Exception e){
            _logger.LogError("GetSurveys Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<List<SurveyListDto>>> GetSurveys(){
        var response = OperationResult.CreateResult<List<SurveyListDto>>();
        try{
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _surveyRepository.GetAll(true)
                .Where(s => s.Status != (int)EntityStatus.Deleted);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var surveys = await query.ToListAsync();
            response.Result = _mapper.Map<List<SurveyListDto>>(surveys);
        } catch(Exception e){
            _logger.LogError("GetSurveys Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<Paging<List<SurveyAnswerListDto>>>> GetSurveyAnswers(int surveyId, PageSetting pager){
        var response = OperationResult.CreateResult<Paging<List<SurveyAnswerListDto>>>();
        try{
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            // Verify survey access
            var surveyQuery = _surveyRepository.GetAll(true).IgnoreQueryFilters().Where(s => s.Id == surveyId);
            surveyQuery = _roleFilter.ApplyFilter(surveyQuery, _context.DbContext);
            if (!await surveyQuery.AnyAsync())
            {
                 response.AddError("Anket bulunamadı veya yetkiniz yok.");
                 return response;
            }

            response.Result = await _surveyAnswerRepository.GetAll(true)
                .Include(s => s.SurveyOption)
                .Include(s => s.Company)
                .Include(s => s.User)
                .Where(s => s.SurveyId == surveyId)
                .ToPagedResultAsync<SurveyAnswerListDto>(pager, _mapper);
        } catch(Exception e){
            _logger.LogError("GetSurveyAnswers Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<List<SurveyAnswerStatisticDto>>> GetSurveyAnswerStatistics(int surveyId){
        var response = OperationResult.CreateResult<List<SurveyAnswerStatisticDto>>();
        try{
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }
             // Verify survey access (optional but good)
            var surveyQuery = _surveyRepository.GetAll(true).IgnoreQueryFilters().Where(s => s.Id == surveyId);
            surveyQuery = _roleFilter.ApplyFilter(surveyQuery, _context.DbContext);
            if (!await surveyQuery.AnyAsync())
            {
                 response.AddError("Anket bulunamadı veya yetkiniz yok.");
                 return response;
            }

            response.Result = await _context.GetRepository<SurveyAnswer>().GetAll(true).Where(s => s.SurveyId == surveyId).Include(s => s.SurveyOption).GroupBy(s => new{s.SurveyOptionId, s.SurveyOption.Title}, (g, v) => new SurveyAnswerStatisticDto{Summary = g.Title, TotalCount = v.Count()}).ToListAsync();
        } catch(Exception e){
            _logger.LogError("GetSurveyAnswerStatistics Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<SurveyUpsertDto>> GetSurveyById(int Id){
        var response = OperationResult.CreateResult<SurveyUpsertDto>();
        try{
            if (!await CanView())
            {
                response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                return response;
            }

            var query = _surveyRepository.GetAll(
                predicate: f => f.Id == Id,
                include: q => q.Include(s => s.SurveyOptions),
                ignoreQueryFilters: true);
            
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            
            var survey = await query.FirstOrDefaultAsync();

            if(survey == null){
                var exists = await _surveyRepository.GetAll(predicate: f => f.Id == Id, ignoreQueryFilters: true).AnyAsync();
                if (exists)
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }
                response.AddError("Anket bulunamadı");
                return response;
            }

            if (!await _roleFilter.CanAccessBranchAsync(survey.BranchId ?? 0, _context.DbContext))
            {
                    response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                    return response;
            }

            response.Result = _mapper.Map<SurveyUpsertDto>(survey);
        } catch(Exception e){
            _logger.LogError("GetSurveyById Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
    public async Task<IActionResult<Empty>> UpsertSurvey(AuditWrapDto<SurveyUpsertDto> model){
        var response = OperationResult.CreateResult<Empty>();
        try{
            var dto = model.Dto;
            if (dto.Id.HasValue)
            {
                if (!await CanEdit())
                {
                    response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                    return response;
                }
                
                var query = _surveyRepository.GetAll(
                    predicate: r => r.Id == dto.Id,
                    include: q => q.Include(s => s.SurveyOptions),
                    disableTracking: false,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var survey = await query.FirstOrDefaultAsync();

                if(survey == null){
                    response.AddError("Anket bulunamadı veya yetkiniz yok");
                    return response;
                }
                
                if (!await _roleFilter.CanAccessBranchAsync(survey.BranchId ?? 0, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                // Check for duplicate title in same branch (excluding current entity)
                var duplicate = await _surveyRepository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(s => s.Id != dto.Id && s.Title.ToLower() == dto.Title.ToLower() && s.BranchId == survey.BranchId && s.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{dto.Title}' isimli anket bu şubede zaten mevcut.");
                    return response;
                }

                survey = _mapper.Map(dto, survey);
                survey.ModifiedId = model.UserId;
                survey.ModifiedDate = DateTime.Now;
                
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult != null && !lastResult.IsOk && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        response.AddError($"'{dto.Title}' isimli anket zaten mevcut.");
                        return response;
                    }
                }
            } else{
                if (!await CanCreate())
                {
                    response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                // Check for duplicate title in current branch
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var duplicate = await _surveyRepository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(s => s.Title.ToLower() == dto.Title.ToLower() && s.BranchId == currentBranchId && s.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    response.AddError($"'{dto.Title}' isimli anket bu şubede zaten mevcut.");
                    return response;
                }

                var survey = new Survey();
                survey = _mapper.Map(dto, survey);
                survey.CreatedId = model.UserId;
                survey.CreatedDate = DateTime.Now;
                survey.BranchId = currentBranchId;
                await _surveyRepository.InsertAsync(survey);
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult != null && !lastResult.IsOk && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        response.AddError($"'{dto.Title}' isimli anket zaten mevcut.");
                        return response;
                    }
                }
            }
            
            response.AddSuccess("Successfull");
        } catch(Exception e){
            _logger.LogError("UpsertSurvey Exception " + e);
            if (e is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
            {
                response.AddError($"'{model.Dto.Title}' isimli anket zaten mevcut.");
            }
            else
            {
                response.AddSystemError(e.Message);
            }
        }
        return response;
    }
    public async Task<IActionResult<Empty>> DeleteSurvey(AuditWrapDto<SurveyDeleteDto> model){
        var response = OperationResult.CreateResult<Empty>();
        try{
            if (!await CanDelete())
            {
                response.AddError("Silme yetkiniz bulunmamaktadır.");
                return response;
            }
            
            var query = _context.DbContext.Surveys.IgnoreQueryFilters().Where(f => f.Id == model.Dto.Id);
            query = _roleFilter.ApplyFilter(query, _context.DbContext);
            var survey = await query.FirstOrDefaultAsync();

            if (survey == null)
            {
                response.AddError("Anket bulunamadı veya yetkiniz yok.");
                return response;
            }

            if (!await _roleFilter.CanAccessBranchAsync(survey.BranchId ?? 0, _context.DbContext))
            {
                 response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                 return response;
            }

            survey.Status = (int)EntityStatus.Deleted;
            survey.DeletedDate = DateTime.Now;
            survey.DeletedId = model.UserId;
            
            await _context.SaveChangesAsync();
            response.AddSuccess("Successfull");
        } catch(Exception e){
            _logger.LogError("DeleteSurvey Exception " + e);
            response.AddSystemError(e.Message);
        }
        return response;
    }
}
