using AutoMapper;
using ecommerce.Admin.Domain.Dtos.BannerItemDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Extensions;
using ecommerce.EFCore.UnitOfWork;
namespace ecommerce.Admin.Domain.Concreate
{
    public class BannerItemService : IBannerItemService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<BannerItem> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<BannerItemListDto> _radzenPagerService;
        private List<BannerItemListDto> _categories = new();
        private List<int> _BannerItemIds = new();
        public BannerItemService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<BannerItemListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<BannerItem>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }
        public async Task<IActionResult<Empty>> DeleteBannerItem(AuditWrapDto<BannerItemDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var control = await _context.DbContext.BannerSubItems.AsNoTracking().Where(x => x.BannerItemId == model.Dto.Id && x.Status==1).AnyAsync();
                if (control)
                {
                    rs.AddError("Bu banner i silemezsiniz. Bu banner a ait alt öğeler mevcut");
                    return rs;
                }

            
                await _context.DbContext.BannerItems.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBannerItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpsertBannerItem(AuditWrapDto<BannerItemUpsertDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<BannerItem>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    if (dto.IsVideo)
                    {
                        entity.FileGuid = dto.VideoUrl;
                        entity.Root = dto.VideoUrl!;
                        entity.FileName = dto.VideoUrl;
                    }
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if (current == null)
                    {
                        rs.AddError("Banner item bulunamadı");
                        return rs;
                    }
                    var updated = _mapper.Map<BannerItem>(dto);
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
                    if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBannerItem Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<BannerItemUpsertDto>> GetBannerItemById(int BannerItemId)
        {
            var rs = new IActionResult<BannerItemUpsertDto> { Result = new() };
            try
            {
                var categories = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == BannerItemId);
                var mappedCat = _mapper.Map<BannerItemUpsertDto>(categories);
                if (mappedCat != null)
                {
                    rs.Result = mappedCat;
                }
                else
                    rs.AddError("Banner item Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBannersForParentSelectList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<int> GetBannerItemLastCount(int bannerId)
        {
            var Banneritem = await _context.DbContext.BannerItems.OrderByDescending(x => x.Order).FirstOrDefaultAsync(x => x.BannerId == bannerId && x.Status != 99);
            return (Banneritem?.Order ?? 0) + 1;
        }
        public async Task<IActionResult<List<BannerItemListDto>>> GetBannerItems()
        {
            var rs = new IActionResult<List<BannerItemListDto>> { Result = new List<BannerItemListDto>() };
            try
            {
                var bannerItems = _repository.GetAll(predicate: f => f.Status == 1, include: i => i.Include(f => f.Banner));
                var mappedCats = _mapper.Map<List<BannerItemListDto>>(bannerItems);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0) rs.Result = mappedCats;

                    foreach (var mdl in rs.Result)
                    {
                        mdl.Banner.Name = mdl.Banner.Name + " > " + ((BannerType)mdl.Banner.BannerType).GetDisplayName();
                    }
                }
                else
                    rs.AddError("Banner Item Listesi Alınamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBannersForParentSelectList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<BannerItemListDto>>>> GetBannerItems(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<BannerItemListDto>>> response = new() { Result = new() };
            try
            {
                var bannerItems = await _repository.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active || x.Status == (int)EntityStatus.Passive, include: x => x.Include(f => f.Banner));
                var mappedCats = _mapper.Map<List<BannerItemListDto>>(bannerItems);
               
                if (mappedCats.Count() > 0) response.Result.Data = mappedCats.AsQueryable();
                if (response.Result.Data != null) response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);
                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBanneritems Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
