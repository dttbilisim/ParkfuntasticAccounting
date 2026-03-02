using AutoMapper;
using ecommerce.Admin.Domain.Dtos.EducationDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Helpers;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Admin.Domain.Concreate;
public class EducationService : IEducationService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<EducationCategory> _educationCategoryRepository;
    private readonly IRepository<EducationItems> _educationItemsRepository;
    private readonly IRepository<Education> _educationRepository;
    private readonly IRepository<EducationImages> _educationImageRepository;
    private readonly IRadzenPagerService<EducationCategoryListDto> _radzenPagerService;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "education-contents";
    private List<int> _categoryIds = new();
    public EducationService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<EducationCategoryListDto> radzenPagerService, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _educationCategoryRepository = context.GetRepository<EducationCategory>();
        ;
        _educationRepository = context.GetRepository<Education>();
        _educationItemsRepository = context.GetRepository<EducationItems>();
        _educationImageRepository = context.GetRepository<EducationImages>();
        _radzenPagerService = radzenPagerService;
        _permissionService = permissionService;
    }
    private List<EducationCategoryListDto> _categories = new();
    [AutoMapper.Configuration.Annotations.Ignore]
    public List<int> Ids { get; set; } = new();
    public async Task<IActionResult<List<EducationListDto>>> GetEducation()
    {
        var rs = new IActionResult<List<EducationListDto>> { Result = new List<EducationListDto>() };
        try
        {
            var datas = await _educationRepository.GetAllAsync(predicate: f => f.Status == 1);
            var mapped = _mapper.Map<List<EducationListDto>>(datas);
            if (mapped != null)
            {
                if (mapped.Count > 0) rs.Result = mapped;
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducation Exception " + ex.ToString());
            rs.AddError("Liste Al?namad?");
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Paging<List<EducationListDto>>>> GetEducations(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<EducationListDto>>>();
        try
        {
            response.Result = await _educationRepository.GetAll(true).Where(s => s.Status != (int)EntityStatus.Deleted).ToPagedResultAsync<EducationListDto>(pager, _mapper);
        }
        catch (Exception e)
        {
            _logger.LogError("GetEducations Exception " + e);
            response.AddSystemError(e.Message);
            _context.DbContext.ChangeTracker.Clear();
        }
        return response;
    }
    public async Task<IActionResult<Empty>> UpsertEducation(AuditWrapDto<EducationUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            var dto = model.Dto;
            var entity = _mapper.Map<Education>(dto);
            if (!dto.Id.HasValue)
            {
                entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _educationRepository.InsertAsync(entity);
            }
            else
            {
                entity = await _context.DbContext.Educations.FirstOrDefaultAsync(x => x.Id == dto.Id);
                entity.Name = dto.Name;
                entity.SubText = dto.SubText;
                entity.Description = dto.Description;
                entity.Order = dto.Order;
                entity.ButtonText = dto.ButtonText;
                entity.ButtonUrl = dto.ButtonUrl;
                entity.StartDate = dto.StartDate;
                entity.EndDate = dto.EndDate;
                entity.CategoryId = dto.CategoryId;
                entity.ImageUrl = dto.ImageUrl;
                entity.PubliserName = dto.PubliserName;
                entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                entity.EducationCategoryType = dto.EducationCategoryType;
                entity.IsSlider = dto.IsSlider;
                _educationRepository.Update(entity);
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
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertEducation Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> DeleteEducation(EducationDeleteDto dto)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.Educations.Where(f => f.Id == dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, 1));
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Successfull");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEducation Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<EducationUpsertDto>> GetEducationId(int Id)
    {
        var rs = new IActionResult<EducationUpsertDto> { Result = new() };
        try
        {
            var data = await _educationRepository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
            var mappedData = _mapper.Map<EducationUpsertDto>(data);
            if (mappedData != null)
            {
                rs.Result = mappedData;
            }
            else
            {
                rs.AddError("Kayit Bulunamadı");
                _context.DbContext.ChangeTracker.Clear();
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducationId Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<int> GetEducationLastCount()
    {
        var rs = await _context.DbContext.Educations.OrderByDescending(x => x.Order).FirstOrDefaultAsync(x => x.Status != 99);
        return (rs?.Order ?? 0) + 1;
    }
    public async Task<IActionResult<EducationCategoryUpsertDto>> GetCategoryById(int categoryId)
    {
        var rs = new IActionResult<EducationCategoryUpsertDto> { Result = new() };
        try
        {
            var categories = await _educationCategoryRepository.GetFirstOrDefaultAsync(predicate: f => f.Id == categoryId);
            var mappedCat = _mapper.Map<EducationCategoryUpsertDto>(categories);
            if (mappedCat != null)
            {
                rs.Result = mappedCat;
            }
            else
            {
                rs.AddError("Kategori Bulunamadı");
                _context.DbContext.ChangeTracker.Clear();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCategoriesForParentSelectList Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());
            _context.DbContext.ChangeTracker.Clear();
        }
        return rs;
    }
    public async Task<IActionResult<List<EducationItemsListDto>>> GetEducationItem()
    {
        var rs = new IActionResult<List<EducationItemsListDto>> { Result = new List<EducationItemsListDto>() };
        try
        {
            var datas = await _educationItemsRepository.GetAllAsync(predicate: f => f.Status == 1);
            var mapped = _mapper.Map<List<EducationItemsListDto>>(datas);
            if (mapped != null)
            {
                if (mapped.Count > 0) rs.Result = mapped;
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducationItem Exception " + ex.ToString());
            rs.AddError("Liste Al?namad?");
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Paging<List<EducationItemsListDto>>>> GetEducationItems(PageSetting pager)
    {
        var response = OperationResult.CreateResult<Paging<List<EducationItemsListDto>>>();
        try
        {
            response.Result = await _educationItemsRepository.GetAll(true).Where(s => s.Status != (int)EntityStatus.Deleted).ToPagedResultAsync<EducationItemsListDto>(pager, _mapper);
        }
        catch (Exception e)
        {
            _logger.LogError("GetEducationItems Exception " + e);
            response.AddSystemError(e.Message);
            _context.DbContext.ChangeTracker.Clear();
        }
        return response;
    }
    public async Task<IActionResult<Empty>> UpsertEducationItem(AuditWrapDto<EducationItemsUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            var entity = _mapper.Map<EducationItems>(model.Dto);
            if (model.Dto.Id == 0)
            {
                entity.Status = 1;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _educationItemsRepository.InsertAsync(entity);
            }
            else
            {
                entity = await _context.DbContext.EducationItems.FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                entity.Name = model.Dto.Name;
                entity.SubText = model.Dto.SubText;
                entity.Description = model.Dto.Description;
                entity.Order = model.Dto.Order;
                entity.EducationId = model.Dto.EducationId;
                entity.Status = model.Dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                entity.Url = model.Dto.Url;
                entity.Duration = model.Dto.Duration;
                _educationItemsRepository.Update(entity);
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
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertEducationItem Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> DeleteEducationItem(EducationItemsDeleteDto dto)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.EducationItems.Where(f => f.Id == dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, 1));
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Successfull");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEducationItem Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<EducationItemsUpsertDto>> GetEducationItemId(int Id)
    {
        var rs = new IActionResult<EducationItemsUpsertDto> { Result = new() };
        try
        {
            var data = await _educationItemsRepository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
            var mappedData = _mapper.Map<EducationItemsUpsertDto>(data);
            if (mappedData != null)
            {
                rs.Result = mappedData;
            }
            else
            {
                rs.AddError("Kayit Bulunamadı");
                _context.DbContext.ChangeTracker.Clear();
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducationItemId Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<int> GetEducationItemLastCount()
    {
        var rs = await _context.DbContext.EducationItems.OrderByDescending(x => x.Order).FirstOrDefaultAsync(x => x.Status != 99);
        return (rs?.Order ?? 0) + 1;
    }
    public async Task<IActionResult<List<EducationImagesListDto>>> GetEducationImage()
    {
        var rs = new IActionResult<List<EducationImagesListDto>> { Result = new List<EducationImagesListDto>() };
        try
        {
            var datas = await _educationImageRepository.GetAllAsync(predicate: f => f.Status == 1);
            var mapped = _mapper.Map<List<EducationImagesListDto>>(datas);
            if (mapped != null)
            {
                if (mapped.Count > 0) rs.Result = mapped;
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducationImage Exception " + ex.ToString());
            rs.AddError("Liste Alınamadı");
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public Task<IActionResult<Paging<List<EducationImagesListDto>>>> GetEducationImages(PageSetting pager) { throw new NotImplementedException(); }
    public async Task<IActionResult<Empty>> UpsertEducationItemImage(AuditWrapDto<EducationImagesUpsertDto> model, AuditWrapDto<EducationItemsUpsertDto> educationItemsUpsertDto)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        var newEducationItems = new EducationItems();
        try
        {

            if (model.Dto.EducationItemId == 0)
            {
                var educationItemsUpsertEntity = _mapper.Map<EducationItems>(educationItemsUpsertDto.Dto);

                educationItemsUpsertEntity.Status = 1;
                educationItemsUpsertEntity.CreatedId = model.UserId;
                educationItemsUpsertEntity.CreatedDate = DateTime.Now;

                if (educationItemsUpsertEntity.Description == null)
                {
                    rs.AddError("İçerik Detayı boş olamaz.");
                    return rs;
                }

                var insertResult = await _educationItemsRepository.InsertAsync(educationItemsUpsertEntity);

                newEducationItems = insertResult.Entity;
                await _context.SaveChangesAsync();
                model.Dto.EducationItemId = newEducationItems.Id;
            }

            var entity = _mapper.Map<EducationImages>(model.Dto);
            if (!model.Dto.Id.HasValue)
            {
                entity.Status = 1;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _educationImageRepository.InsertAsync(entity);
            }
            else
            {
                entity = await _context.DbContext.EducationImages.FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                entity!.Order = model.Dto.Order;
                entity.EducationItemId = model.Dto.EducationItemId;
                entity.Status = model.Dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                _educationImageRepository.Update(entity);
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
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertEducationItemImage Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> DeleteEducationItemImage(EducationImagesDeleteDto dto)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.EducationImages.Where(f => f.Id == dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, 1));
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Successfull");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteEducationItemImage Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<List<EducationImagesListDto>>> GetEducationItemImageId(int Id)
    {
        var rs = new IActionResult<List<EducationImagesListDto>> { Result = new() };
        try
        {
            var data = await _educationImageRepository.GetAllAsync(predicate: f => f.EducationItemId == Id && f.Status == 1);
            var mappedData = _mapper.Map<List<EducationImagesListDto>>(data);
            if (mappedData != null)
            {
                rs.Result = mappedData;
            }
            else
            {
                rs.AddError("Kayit Bulunamadı");
                _context.DbContext.ChangeTracker.Clear();
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetEducationItemImageId Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<int> GetEducationItemImageLastCount()
    {
        var rs = await _context.DbContext.EducationImages.OrderByDescending(x => x.Order).FirstOrDefaultAsync(x => x.Status != 99);
        return (rs?.Order ?? 0) + 1;
    }
    public async Task<IActionResult<List<EducationCategoryListDto>>> GetTreeCategories()
    {
        _categories = new();
        var rs = new IActionResult<List<EducationCategoryListDto>> { Result = new List<EducationCategoryListDto>() };
        try
        {
            //Parent categories
            var categories = await _educationCategoryRepository.GetAllAsync(predicate: f => f.Status == (int)EntityStatus.Active);
            var mappedCats = _mapper.Map<List<EducationCategoryListDto>>(categories.Where(x => x.ParentId == null));
            var mappedAllCats = _mapper.Map<List<EducationCategoryListDto>>(categories);
            foreach (var item in mappedCats)
            {
                item.Ids.Add(item.Id);
                await GetCategoryTree(mappedAllCats, item);
            }
            rs.Result = _categories;
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetTreeCategories Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task GetCategoryTree(List<EducationCategoryListDto> list, EducationCategoryListDto item)
    {
        _categories.Add(item);
        var categories = list.Where(x => x.Status == 1 && x.ParentId == item.Id);
        foreach (var cat in categories)
        {
            cat.Name = item.Name + " >> " + cat.Name;
            cat.Ids.AddRange(new List<int>() { item.Id, cat.Id });
            cat.Ids.AddRange(item.Ids);
            await GetCategoryTree(list, cat);
        }
    }
    public async Task<IActionResult<List<EducationCategoryListDto>>> GetCategoriesByEducationCategoryType(EducationCategoryType educationCategoryType)
    {
        var rs = new IActionResult<List<EducationCategoryListDto>> { Result = new List<EducationCategoryListDto>() };
        try
        {
            var categories = await _educationCategoryRepository.GetAllAsync(predicate: f => f.Status == 1 && f.EducationCategoryType == educationCategoryType, include: i => i.Include(f => f.Parent!));
            var mappedCats = _mapper.Map<List<EducationCategoryListDto>>(categories);
            if (mappedCats != null)
            {
                if (mappedCats.Count > 0) rs.Result = mappedCats;
            }
            else
            {
                rs.AddError("Kategori Listesi Alınamadı");
                _context.DbContext.ChangeTracker.Clear();
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCategoriesForParentSelectList Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Paging<IQueryable<EducationCategoryListDto>>>> GetCategories(PageSetting pager)
    {
        IActionResult<Paging<IQueryable<EducationCategoryListDto>>> response = new() { Result = new() };
        try
        {
            var categories = await _educationCategoryRepository.GetAllAsync(predicate: x => x.Status != (int)EntityStatus.Deleted, include: x => x.Include(f => f.Parent!));
            var mappedCats = _mapper.Map<List<EducationCategoryListDto>>(categories);
            foreach (var item in mappedCats)
            {
                _categories = new();
                await GetParentCategoryTree(mappedCats, item);
                if (item.Parent != null) item.Parent.Name = string.Join(" >> ", _categories.Select(x => x?.Name));
            }
            if (mappedCats.Count() > 0) response.Result.Data = mappedCats.AsQueryable();
            if (response.Result.Data != null) response.Result.Data = response.Result.Data!.OrderByDescending(x => x.Id);
            var result = _radzenPagerService.MakeDataQueryable(response.Result.Data!, pager);
            response.Result = result;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCategories Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return response;
        }
    }
    public async Task<IActionResult<Empty>> UpsertCategory(AuditWrapDto<EducationCategoryUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            var dto = model.Dto;
            var entity = _mapper.Map<EducationCategory>(dto);
            if (!dto.Id.HasValue)
            {
                entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;
                await _educationCategoryRepository.InsertAsync(entity);
            }
            else
            {
                entity = await _context.DbContext.EducationCategories.FirstOrDefaultAsync(x => x.Id == dto.Id);
                entity!.Name = dto.Name;
                entity.ParentId = dto.ParentId;
                entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.ModifiedId = model.UserId;
                entity.ModifiedDate = DateTime.Now;
                entity.Order = dto.Order;
                entity.EducationCategoryType = dto.EducationCategoryType;
                _educationCategoryRepository.Update(entity);
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
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("UpsertCategory Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task<IActionResult<Empty>> DeleteCategory(AuditWrapDto<EducationCategoryDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.EducationCategories.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Successfull");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception!.ToString());

                _context.DbContext.ChangeTracker.Clear();
                return rs;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteCategory Exception " + ex.ToString());
            rs.AddSystemError(ex.ToString());

            _context.DbContext.ChangeTracker.Clear();
            return rs;
        }
    }
    public async Task GetParentCategoryTree(List<EducationCategoryListDto> list, EducationCategoryListDto item)
    {
        if (item is not null && item.ParentId is not null)
        {
            if (!_categories.Any(x => x.Id == item.Id)) _categories.Add(item);
            var parentCategory = list.FirstOrDefault(x => x.Id == item.ParentId);
            if (parentCategory != null)
            {
                _categories.Add(parentCategory);
                await GetParentCategoryTree(list, parentCategory);
            }
        }
        _categories = _categories.OrderBy(x => x?.Id).ToList();
    }
}
