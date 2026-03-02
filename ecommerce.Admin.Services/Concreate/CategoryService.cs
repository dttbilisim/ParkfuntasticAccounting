using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Interfaces;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Npgsql;
namespace ecommerce.Admin.Domain.Concreate{
    public class CategoryService : ICategoryService{
      
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Category> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<CategoryListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "categories";
        private List<CategoryListDto> _categories;

        public CategoryService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<CategoryListDto> radzenPagerService, IServiceScopeFactory serviceScopeFactory, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<Category>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }
        public async Task<IActionResult<Empty>> DeleteCategory(AuditWrapDto<CategoryDeleteDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                var productControl = await _context.DbContext.ProductCategories.AsNoTracking().Where(x => x.CategoryId == model.Dto.Id).AnyAsync();
                if(productControl){
                    rs.AddError("Bu kategoriyi silemezsiniz. Bu kategoriye ait ürünler mevcut");
                    return rs;
                }

                var branchId = _tenantProvider.GetCurrentBranchId();
                await _context.DbContext.Category.Where(f => f.Id == model.Dto.Id && f.BranchId == branchId).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    rs.AddSuccess("Successfull");
                    return rs;
                } else{
                    if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            } catch(Exception ex){
                _logger.LogError("DeleteCategory Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpsertCategory(AuditWrapDto<CategoryUpsertDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                var dto = model.Dto;
                var branchId = _tenantProvider.GetCurrentBranchId();
                if(!dto.Id.HasValue){
                    // Check for duplicate name in current branch
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.BranchId == branchId && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kategori bu şubede zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<Category>(dto);
                    entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    entity.BranchId = branchId;
                    await _repository.InsertAsync(entity);
                } else{
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted && x.BranchId == branchId,
                        disableTracking: true);
                    if(current == null){
                        rs.AddError("Kategori bulunamadı");
                        return rs;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Id != dto.Id && c.Name.ToLower() == dto.Name.ToLower() && c.BranchId == branchId && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kategori bu şubede zaten mevcut.");
                        return rs;
                    }

                    var updated = _mapper.Map<Category>(dto);
                    updated.Id = current.Id;
                    updated.BranchId = current.BranchId; 
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
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
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            rs.AddError($"'{dto.Name}' isimli kategori zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                        }
                        else
                        {
                            rs.AddError(lastResult.Exception.ToString());
                        }
                    }
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertCategory Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli kategori zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    rs.AddSystemError(ex.ToString());
                }
                return rs;
            }
        }
        public async Task<IActionResult<CategoryUpsertDto>> GetCategoryById(int categoryId){
            var rs = new IActionResult<CategoryUpsertDto>{Result = new()};
            try{
                var branchId = _tenantProvider.GetCurrentBranchId();
                var categories = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == categoryId && f.BranchId == branchId);
                var mappedCat = _mapper.Map<CategoryUpsertDto>(categories);
                if(mappedCat != null){
                    rs.Result = mappedCat;
                } else
                    rs.AddError("Kategori Bulunamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCategoriesForParentSelectList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<CategoryListDto>>> GetCategories(){
            var rs = new IActionResult<List<CategoryListDto>>{Result = new List<CategoryListDto>()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Category>();
                    
                    var query = repository.GetAll(disableTracking: true)
                        .Include(f => f.Parent)
                        .Where(f => f.Status == 1);

                    query = _roleFilter.ApplyFilter(query, context.DbContext);

                    var categories = await query.ToListAsync();
                    var mappedCats = _mapper.Map<List<CategoryListDto>>(categories);
                    if(mappedCats != null){
                        if(mappedCats.Count > 0) rs.Result = mappedCats;
                    } else
                        rs.AddError("Kategori Listesi Alınamadı");
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCategories Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Paging<IQueryable<CategoryListDto>>>> GetCategories(PageSetting pager){
            IActionResult<Paging<IQueryable<CategoryListDto>>> response = new(){Result = new()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Category>();
                    
                    List<int> allowedBranchIds = new();
                    if (!isGlobalAdmin && userId > 0)
                    {
                         allowedBranchIds = context.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }

                    var categories = await repository.GetAllAsync(
                        predicate: x => x.Status == (int) EntityStatus.Active 
                            && (
                                 isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) :
                                 (allowedBranchIds.Contains(x.BranchId) && (currentBranchId == 0 || x.BranchId == currentBranchId))
                            ), 
                        include: x => x.Include(f => f.Parent),
                        disableTracking: true);
                    var mappedCats = _mapper.Map<List<CategoryListDto>>(categories);
                    foreach(var item in mappedCats){
                        _categories = new();
                        await GetParentCategoryTree(mappedCats, item);
                        if(_categories.Count() > 0) item.Parent.Name = string.Join(" >> ", _categories.Select(x => x?.Name));
                    }
                    if(mappedCats.Count() > 0) response.Result.Data = mappedCats.AsQueryable();
                    if(response.Result.Data != null) response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);
                    var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                    response.Result = result;
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetCategories Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<CategoryListDto>>> GetTreeCategories(){
            _categories = new();
            var rs = new IActionResult<List<CategoryListDto>>{Result = new List<CategoryListDto>()};
            try{
                var query = _repository.GetAll(ignoreQueryFilters: true)
                    .Where(f => f.Status == (int)EntityStatus.Active);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                var categories = await query.ToListAsync();
                var mappedCats = _mapper.Map<List<CategoryListDto>>(categories.Where(x => x.ParentId == null));
                var mappedAllCats = _mapper.Map<List<CategoryListDto>>(categories);
                foreach(var item in mappedCats){
                    item.Ids.Add(item.Id);
                    await GetCategoryTree(mappedAllCats, item);
                }
                rs.Result = _categories;
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetTreeCategories Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task GetCategoryTree(List<CategoryListDto> list, CategoryListDto item){
            _categories.Add(item);
            var categories = list.Where(x => x.Status == 1 && x.ParentId == item.Id);
            foreach(var cat in categories){
                cat.Name = item.Name + " >> " + cat.Name;
                cat.Ids.AddRange(new List<int>(){item.Id, cat.Id});
                cat.Ids.AddRange(item.Ids);
                await GetCategoryTree(list, cat);
            }
        }
        public async Task GetParentCategoryTree(List<CategoryListDto> list, CategoryListDto item){
            if(item is not null && item.ParentId is not null){
                if(!_categories.Any(x => x.Id == item.Id)) _categories.Add(item);
                var parentCategory = list.FirstOrDefault(x => x.Id == item.ParentId);
                _categories.Add(parentCategory);
            }
            _categories = _categories.OrderBy(x => x?.Id)?.ToList();
            return;
        }
        public async Task<IActionResult<List<Category>>> GetCategoryHierarchy()
        {
            var rs = new IActionResult<List<Category>> { Result = new List<Category>() };
            try
            {
                var query = _repository.GetAll(ignoreQueryFilters: true)
                    .Where(x => x.Status != (int)EntityStatus.Deleted);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                var allCategories = await query.ToListAsync();
                
                var categories = allCategories.ToList(); 
                
                foreach(var cat in categories) cat.SubCategories = new List<Category>(); 
                
                var rootCategories = categories.Where(x => x.ParentId == null || x.ParentId == 0).ToList();
                
                foreach(var cat in categories.Where(x => x.ParentId.HasValue && x.ParentId > 0))
                {
                        var parent = categories.FirstOrDefault(p => p.Id == cat.ParentId);
                        if (parent != null) parent.SubCategories.Add(cat);
                        else if(!rootCategories.Contains(cat)) rootCategories.Add(cat);
                }

                SortCategories(rootCategories);
                rs.Result = rootCategories;
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCategoryHierarchy error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }
        
        private void SortCategories(List<Category> categories)
        {
            if (categories == null) return;
            categories.Sort((a, b) => 
            {
                    int orderComparison = a.Order.CompareTo(b.Order);
                    if (orderComparison != 0) return orderComparison;
                    return a.Id.CompareTo(b.Id);
            });
            
            foreach(var cat in categories)
            {
                if(cat.SubCategories != null && cat.SubCategories.Any())
                {
                    var subList = cat.SubCategories.ToList();
                    SortCategories(subList);
                    cat.SubCategories = subList;
                }
            }
        }
        
        public async Task<IActionResult<Empty>> UpdateCategoryLocation(int id, int? parentId, int order)
        {
             var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Category>();
                    
                    var branchId = _tenantProvider.GetCurrentBranchId();
                    var category = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == id && x.BranchId == branchId,
                        disableTracking: false
                    );

                    if (category == null)
                    {
                        rs.AddError("Kategori bulunamadı");
                        return rs;
                    }

                    var oldParentId = category.ParentId;
                    category.ParentId = parentId == 0 ? null : parentId;
                    
                    var siblings = await repo.GetAllAsync(predicate: x => x.ParentId == category.ParentId && x.Id != id && x.Status != (int)EntityStatus.Deleted && x.BranchId == branchId);
                    var siblingList = siblings.OrderBy(x => x.Order).ToList();
                    
                    if (oldParentId == parentId && category.Order < order)
                    {
                        order--;
                    }

                    if (order < 0) order = 0;
                    if (order > siblingList.Count) order = siblingList.Count;
                    
                    siblingList.Insert(order, category);
                    
                    for (int i = 0; i < siblingList.Count; i++)
                    {
                         siblingList[i].Order = i;
                         repo.Update(siblingList[i]);
                    }

                    await uow.SaveChangesAsync();
                }
                rs.AddSuccess("Kategori güncellendi");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCategoryLocation error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

    }
}
