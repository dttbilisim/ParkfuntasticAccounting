using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SearchSynonymDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Domain.Concreate
{
    public class SearchSynonymAdminService : ISearchSynonymAdminService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<SearchSynonym> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<SearchSynonymAdminService> _logger;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "search-synonyms";

        public SearchSynonymAdminService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger<SearchSynonymAdminService> logger, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<SearchSynonym>();
            _mapper = mapper;
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Paging<List<SearchSynonymListDto>>>> GetSynonyms(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<SearchSynonymListDto>>>();
            try
            {
                response.Result = await _repository.GetAll(true)
                    .Where(s => s.Status != (int)EntityStatus.Deleted)
                    .OrderByDescending(x => x.CreatedDate)
                    .ToPagedResultAsync<SearchSynonymListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetSynonyms Exception");
                response.AddSystemError(e.Message);
            }
            return response;
        }

        public async Task<IActionResult<SearchSynonymUpsertDto>> GetSynonymById(int id)
        {
            var rs = new IActionResult<SearchSynonymUpsertDto> { Result = new SearchSynonymUpsertDto() };
            try
            {
                var entry = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == id);
                if (entry != null)
                {
                    rs.Result = _mapper.Map<SearchSynonymUpsertDto>(entry);
                    rs.Result.StatusBool = rs.Result.Status == (int)EntityStatus.Active;
                }
                else rs.AddError("Eş anlamlı bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetSynonymById Exception");
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }

        public async Task<IActionResult<Empty>> UpsertSynonym(AuditWrapDto<SearchSynonymUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue || dto.Id == 0)
                {
                    var entity = _mapper.Map<SearchSynonym>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                    rs.AddSuccess("Başarıyla eklendi");
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Id == dto.Id, disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Eş anlamlı bulunamadı");
                        return rs;
                    }
                    var updated = _mapper.Map<SearchSynonym>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    rs.AddSuccess("Başarıyla güncellendi");
                }

                await _context.SaveChangesAsync();
                if (!_context.LastSaveChangesResult.IsOk)
                {
                    rs.AddError(_context.LastSaveChangesResult.Exception?.Message ?? "Kaydedilirken bir hata oluştu");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertSynonym Exception");
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }

        public async Task<IActionResult<Empty>> DeleteSynonym(AuditWrapDto<SearchSynonymDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                await _context.DbContext.Set<SearchSynonym>()
                    .Where(f => f.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now)
                        .SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                rs.AddSuccess("Başarıyla silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteSynonym Exception");
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }
    }
}
