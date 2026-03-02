using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SupportLineDto;
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

namespace ecommerce.Admin.Domain.Concreate;

public class SupportLineService : ISupportLineService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<SupportLine> _supportLineRepository;
    private readonly IFrequentlyAskedQuestionService _repositoryFrequentlyAskedQuestion;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "supportline";

    public SupportLineService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
       IFrequentlyAskedQuestionService repositoryFrequentlyAskedQuestion,
        CurrentUser currentUser,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _repositoryFrequentlyAskedQuestion= repositoryFrequentlyAskedQuestion;
        _supportLineRepository = context.GetRepository<SupportLine>();
        _permissionService = permissionService;
    }

    public async Task<IActionResult<Paging<List<SupportLineListDto>>>> GetSupportLines(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<SupportLineListDto>>>();
      
        try
        {
            response.Result = await _supportLineRepository.GetAll(true)
                .Where(s => s.Status != (int) EntityStatus.Deleted)
                .ToPagedResultAsync<SupportLineListDto>(pager, _mapper);            
        }
        catch (Exception e)
        {
            _logger.LogError("GetSupportLines Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<List<SupportLineListDto>>> GetSupportLines()
    {
        var response = OperationResult.CreateResult<List<SupportLineListDto>>();

        try
        {
            var supportLines = await _supportLineRepository.GetAllAsync(predicate: s => s.Status != (int) EntityStatus.Deleted);

            response.Result = _mapper.Map<List<SupportLineListDto>>(supportLines);
        }
        catch (Exception e)
        {
            _logger.LogError("GetSupportLines Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<SupportLineUpsertDto>> GetSupportLineById(int Id)
    {
        var response = OperationResult.CreateResult<SupportLineUpsertDto>();
        try
        {
            var supportLine = await _supportLineRepository.GetFirstOrDefaultAsync(
                predicate: f => f.Id == Id && f.Status != (int)EntityStatus.Deleted
            );           
            if (supportLine == null)
            {
                response.AddError("Destek kaydı bulunamadı");
                return response;
            }
            var map = _mapper.Map<SupportLineUpsertDto>(supportLine);
            var subject = await _repositoryFrequentlyAskedQuestion.GetFrequentlyAskedQuestionById(map.FrequentlyAskedQuestionsId);
            map.FrequentlyAskedQuestionName = subject?.Result?.Name;
            response.Result = map;
            
        }
        catch (Exception e)
        {
            _logger.LogError("GetSupportLineById Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> UpsertSupportLine(SupportLineUpsertDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            var supportLine = await _supportLineRepository.GetFirstOrDefaultAsync(
                predicate: r => r.Id == dto.Id,
                disableTracking: false
            );

            if (supportLine == null)
            {
                response.AddError("Destek kaydı bulunamadı");
                return response;
            }

            supportLine.Note = dto.Note;

            supportLine.ModifiedId = _currentUser.GetId();
            supportLine.ModifiedDate = DateTime.Now;

            _supportLineRepository.Update(supportLine);

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("UpsertSupportLine Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> DeleteSupportLine(SupportLineDeleteDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            await _supportLineRepository.GetAll(true)
                .Where(f => f.Id == dto.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now)
                        .SetProperty(a => a.DeletedId, _currentUser.GetId())
                );
        }
        catch (Exception e)
        {
            _logger.LogError("DeleteSupportLine Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }
}