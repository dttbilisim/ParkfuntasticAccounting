using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BannerSubItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.Extensions.Logging;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Concreate
{
    public class BannerSubItemService : IBannerSubItemService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<BannerSubItem> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<BannerSubItemListDto> _radzenPagerService;

        public BannerSubItemService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<BannerSubItemListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<BannerSubItem>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }
        public async Task<IActionResult<Paging<List<BannerSubItemListDto>>>> GetBannerSubItems(PageSetting pager,int banneritemId)
        {
            var response = OperationResult.CreateResult<Paging<List<BannerSubItemListDto>>>();

            try
            {
                response.Result = await _repository.GetAll(true).Where(x=>x.BannerItemId==banneritemId)
                    .Where(s => s.Status != (int)EntityStatus.Deleted)
                    .ToPagedResultAsync<BannerSubItemListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBannerSubItems Exception " + e);
                response.AddSystemError(e.Message);
            }

            return response;
        }

        public async Task<IActionResult<Empty>> DeleteBannerSubItem(AuditWrapDto<BannerSubItemDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                 
                //Deleted Mark with audit
                await _context.DbContext.BannerSubItems.Where(f => f.Id == model.Dto.Id).
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
                _logger.LogError("DeleteBannerSubItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<BannerSubItemUpsertDto>> GetBannerSubItemById(int Id)
        {
            var rs = new IActionResult<BannerSubItemUpsertDto>
            {
                Result = new BannerSubItemUpsertDto()
            };
            try
            {
                var BannerSubItem = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mappedCat = _mapper.Map<BannerSubItemUpsertDto>(BannerSubItem);
                if (mappedCat != null)
                {
                    rs.Result = mappedCat;
                }
                else rs.AddError("BannerSubItem Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBannerSubItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertBannerSubItem(AuditWrapDto<BannerSubItemUpsertDto> model)
        {
            //The instance of entity type 'BannerSubItem' cannot be tracked because another instance with the same key value for { 'Id'} is already being tracked.When attaching existing entities, ensure that only one entity instance with a given key value is attached.Consider using


            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<BannerSubItem>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Alt banner öğesi bulunamadı");
                        return rs;
                    }
                    var updated = _mapper.Map<BannerSubItem>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                }
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
                _logger.LogError("UpsertBannerSubItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<BannerSubItemListDto>>> GetBannerSubItems()
        {
            IActionResult<List<BannerSubItemListDto>> response = new() { Result = new() };
            try
            {

                var BannerSubItems = _repository.GetAll(predicate: f => f.Status == (int)EntityStatus.Active);
                var mappedCats = _mapper.Map<List<BannerSubItemListDto>>(BannerSubItems);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0)
                        response.Result = mappedCats.ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductForParentSelectList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
