using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ActiveArticleDto;
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
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class ActiveArticlesService : IActiveArticlesService
    {

        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<ActiveArticle> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<ActiveArticleListDto> _radzenPagerService;

        public ActiveArticlesService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<ActiveArticleListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<ActiveArticle>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }


        public async Task<IActionResult<Empty>> DeleteActiveArticle(AuditWrapDto<ActiveArticleDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                await _context.DbContext.ActiveArticles.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
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
                _logger.LogError("DeleteActiveArticle Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<ActiveArticleUpsertDto>> GetActiveArticleById(int Id)
        {
            var rs = new IActionResult<ActiveArticleUpsertDto>
            {
                Result = new()
            };
            try
            {
                var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<ActiveArticleUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Etkin Madde Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetActiveArticleById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<ActiveArticleListDto>>>> GetActiveArticles(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<ActiveArticleListDto>>> response = new() { Result = new() };
            try
            {

                var entities = await _repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active);
                var mappedList = _mapper.Map<List<ActiveArticleListDto>>(entities) ?? new List<ActiveArticleListDto>();
                
                response.Result.Data = mappedList.AsQueryable().OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);


                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetActiveArticles Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<ActiveArticleListDto>>> GetActiveArticles()
        {
            var rs = new IActionResult<List<ActiveArticleListDto>>
            {
                Result = new()
            };
            try
            {
                var entities = _repository.GetAll(predicate: x=> x.Status == (int)EntityStatus.Active);
                var mapped = _mapper.Map<List<ActiveArticleListDto>>(entities);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;
                }

                else rs.AddError("Etkin Madde Listesi Alınamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetActiveArticles Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertActiveArticle(AuditWrapDto<ActiveArticleUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<ActiveArticle>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.ActiveArticles.Where(x => x.Id == model.Dto.Id)
                        .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.Name, model.Dto.Name)
                        .SetProperty(c => c.Status, dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));
                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
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
                _logger.LogError("UpsertActiveArticle Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
