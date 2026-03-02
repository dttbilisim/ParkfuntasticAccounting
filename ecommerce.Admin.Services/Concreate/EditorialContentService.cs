using AutoMapper;
using ecommerce.Admin.Domain.Dtos.EditorialContentDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Extensions;
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

public class EditorialContentService : IEditorialContentService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<EditorialContent> _editorialContentRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly CurrentUser _currentUser;
    private readonly FileHelper _fileHelper;

    public EditorialContentService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        CurrentUser currentUser,
        FileHelper fileHelper)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _fileHelper = fileHelper;

        _editorialContentRepository = context.GetRepository<EditorialContent>();
    }

    public async Task<IActionResult<Paging<List<EditorialContentListDto>>>> GetEditorialContents(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<EditorialContentListDto>>>();

        try
        {
            response.Result = await _editorialContentRepository.GetAll(true)
                .Where(s => s.Status != (int) EntityStatus.Deleted)
                .ToPagedResultAsync<EditorialContentListDto>(pager, _mapper);
        }
        catch (Exception e)
        {
            _logger.LogError("GetEditorialContents Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<List<EditorialContentListDto>>> GetEditorialContents()
    {
        var response = OperationResult.CreateResult<List<EditorialContentListDto>>();

        try
        {
            var editorialContents = await _editorialContentRepository.GetAllAsync(predicate: s => s.Status != (int) EntityStatus.Deleted);

            response.Result = _mapper.Map<List<EditorialContentListDto>>(editorialContents);
        }
        catch (Exception e)
        {
            _logger.LogError("GetEditorialContents Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<EditorialContentUpsertDto>> GetEditorialContentById(int id)
    {
        var response = OperationResult.CreateResult<EditorialContentUpsertDto>();

        try
        {
            var editorialContent = await _editorialContentRepository.GetFirstOrDefaultAsync(
                predicate: f => f.Id == id && f.Status != (int) EntityStatus.Deleted
            );

            if (editorialContent == null)
            {
                response.AddError("İçerik bulunamadı");
                return response;
            }

            response.Result = _mapper.Map<EditorialContentUpsertDto>(editorialContent);

            response.Result.Content = HtmlHelper.ModifyHtmlContentImages(_fileHelper, response.Result.Content);
        }
        catch (Exception e)
        {
            _logger.LogError("GetEditorialContentById Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> UpsertEditorialContent(EditorialContentUpsertDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            var editorialContent = dto.Id.HasValue
                ? await _editorialContentRepository.GetFirstOrDefaultAsync(
                    predicate: r => r.Id == dto.Id,
                    disableTracking: false
                )
                : new EditorialContent();

            if (editorialContent == null)
            {
                response.AddError("İçerik bulunamadı");
                return response;
            }

            editorialContent = _mapper.Map(dto, editorialContent);

            var slug = dto.Slug?.ToFriendlyTitle();

            if (string.IsNullOrEmpty(slug))
            {
                slug = dto.Title.ToFriendlyTitle();
            }

            editorialContent.Slug = slug;

            editorialContent.Content = HtmlHelper.ModifyHtmlContentImages(_fileHelper, editorialContent.Content, true);

            if (await _editorialContentRepository.GetAll(true).AnyAsync(s => s.Slug == editorialContent.Slug && s.Id != editorialContent.Id))
            {
                response.AddError("Aynı url başka bir içerikte kullanılmaktadır.");
                return response;
            }
            

            if (editorialContent.Id > 0)
            {
                editorialContent.ModifiedId = _currentUser.GetId();
                editorialContent.ModifiedDate = DateTime.Now;
                

                _editorialContentRepository.Update(editorialContent);
            }
            else
            {
                editorialContent.CreatedId = _currentUser.GetId();
                editorialContent.CreatedDate = DateTime.Now;

                await _editorialContentRepository.InsertAsync(editorialContent);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("GetEditorialContents Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }

    public async Task<IActionResult<Empty>> DeleteEditorialContent(EditorialContentDeleteDto dto)
    {
        var response = OperationResult.CreateResult<Empty>();

        try
        {
            await _editorialContentRepository.GetAll(true)
                .Where(f => f.Id == dto.Id)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now)
                        .SetProperty(a => a.DeletedId, _currentUser.GetId())
                );
        }
        catch (Exception e)
        {
            _logger.LogError("DeleteEditorialContent Exception " + e);
            response.AddSystemError(e.Message);
        }

        return response;
    }
}