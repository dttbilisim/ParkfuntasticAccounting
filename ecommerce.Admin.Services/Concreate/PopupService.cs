using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PopupDto;
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

public class PopupService : IPopupService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<Popup> _popupRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly FileHelper _fileHelper;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "popups";

    public PopupService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        CurrentUser currentUser,
        FileHelper fileHelper,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _fileHelper = fileHelper;

        _popupRepository = context.GetRepository<Popup>();
        _permissionService = permissionService;
    }

    public async Task<IActionResult<Paging<List<PopupListDto>>>> GetPopups(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<PopupListDto>>>();

        try
        {
            response.Result = await _popupRepository.GetAll(true)
                .Where(s => s.Status != (int) EntityStatus.Deleted)
                .ToPagedResultAsync<PopupListDto>(pager, _mapper);
        }
        catch (Exception e)
        {
            _logger.LogError("GetPopups Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<List<PopupListDto>>> GetPopups()
    {
        var response = OperationResult.CreateResult<List<PopupListDto>>();

        try
        {
            var popups = await _popupRepository.GetAllAsync(predicate: s => s.Status != (int) EntityStatus.Deleted);

            response.Result = _mapper.Map<List<PopupListDto>>(popups);
        }
        catch (Exception e)
        {
            _logger.LogError("GetPopups Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<PopupUpsertDto>> GetPopupById(int Id)
    {
        var response = OperationResult.CreateResult<PopupUpsertDto>();

        try
        {
            var popup = await _popupRepository.GetFirstOrDefaultAsync(
                predicate: f => f.Id == Id && f.Status != (int) EntityStatus.Deleted
            );

            if (popup == null)
            {
                response.AddError("Popup bulunamadı");
                return response;
            }

            response.Result = _mapper.Map<PopupUpsertDto>(popup);

            response.Result.Body = HtmlHelper.ModifyHtmlContentImages(_fileHelper, response.Result.Body);
        }
        catch (Exception e)
        {
            _logger.LogError("GetPopupById Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> UpsertPopup(PopupUpsertDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            var popup = dto.Id.HasValue
                ? await _popupRepository.GetFirstOrDefaultAsync(
                    predicate: r => r.Id == dto.Id,
                    disableTracking: false
                )
                : new Popup();

            if (popup == null)
            {
                response.AddError("Popup bulunamadı");
                return response;
            }

            popup = _mapper.Map(dto, popup);

            popup.Body = HtmlHelper.ModifyHtmlContentImages(_fileHelper, popup.Body, true);

            if (popup.Id > 0)
            {
                popup.ModifiedId = _currentUser.GetId();
                popup.ModifiedDate = DateTime.Now;

                _popupRepository.Update(popup);
            }
            else
            {
                popup.CreatedId = _currentUser.GetId();
                popup.CreatedDate = DateTime.Now;

                await _popupRepository.InsertAsync(popup);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("GetPopups Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> DeletePopup(PopupDeleteDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            await _popupRepository.GetAll(true)
                .Where(f => f.Id == dto.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now)
                        .SetProperty(a => a.DeletedId, _currentUser.GetId())
                );
        }
        catch (Exception e)
        {
            _logger.LogError("DeletePopup Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }
}