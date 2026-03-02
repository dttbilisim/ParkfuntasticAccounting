using System.Globalization;
using AutoMapper;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Report;
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
using System.Data;
using System.Text;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace ecommerce.Admin.Domain.Concreate{
    public class ProductService : IProductService{
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Product> _repository;
        private readonly IRepository<ProductOnline> _repositoryOnline;
        private readonly IRepository<ProductTransaction> _productTransactionRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private IReportService _reportService{get;set;}
        private readonly IRadzenPagerService<ProductListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IDapperService _dapperService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "products";

        public ProductService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<ProductListDto> radzenPagerService, IReportService reportService, IServiceScopeFactory serviceScopeFactory, IDapperService dapperService, ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService){
            _context = context;
            _repository = context.GetRepository<Product>();
            _repositoryOnline = context.GetRepository<ProductOnline>();
            _productTransactionRepository = context.GetRepository<ProductTransaction>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _reportService = reportService;
            _serviceScopeFactory = serviceScopeFactory;
            _dapperService = dapperService;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }
        public async Task<IActionResult<Empty>> DeleteProduct(AuditWrapDto<ProductDeleteDto> model){
            var response = new IActionResult<Empty>{Result = new Empty()};
            try{
                if(await _context.DbContext.ProductSellerItems.AnyAsync(x => x.ProductId == model.Dto.Id && x.Status == 1)){
                    response.AddError("Bu ürünü silemezsiniz. Bu ürüne ait ilanlar mevcut");
                    return response;
                }
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var product = await _context.DbContext.Product.FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                
                if (product != null)
                {
                    if (!isGlobalAdmin && (currentBranchId == 0 || product.BranchId != currentBranchId))
                    {
                         // Check if user is allowed for this branch specifically
                         var user = _httpContextAccessor.HttpContext?.User;
                         var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                         if (int.TryParse(userIdClaim, out int userId))
                         {
                             var isAllowed = await _context.DbContext.UserBranches
                                .AnyAsync(ub => ub.UserId == userId && ub.BranchId == product.BranchId && ub.Status == (int)EntityStatus.Active);
                             if (!isAllowed)
                             {
                                 response.AddError("Bu ürünü silme yetkiniz yok.");
                                 return response;
                             }
                         }
                    }
                    _context.DbContext.Product.Remove(product);
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    response.AddSuccess("Successfull");
                    return response;
                }
                if(lastResult != null && lastResult.Exception != null) response.AddError(lastResult.Exception.ToString());
                return response;
            } catch(Exception ex){
                _logger.LogError($"DeleteProduct Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<ProductUpsertDto>> GetProductById(int productId)
        {
            IActionResult<ProductUpsertDto> response = new IActionResult<ProductUpsertDto> { Result = new() };
            try
            {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var context = _context;
                    var repository = context.GetRepository<Product>();
                    var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();

                    var product = await repository.GetFirstOrDefaultAsync(
                        predicate: f => f.Id == productId 
                            && (isGlobalAdmin ? (currentBranchId == 0 || f.BranchId == currentBranchId) : true), 
                        ignoreQueryFilters: true);
                    
                    if (product != null && !isGlobalAdmin)
                    {
                        var user = _httpContextAccessor.HttpContext?.User;
                        var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (int.TryParse(userIdClaim, out int userId))
                        {
                             var allowedBranchIds = await context.DbContext.UserBranches
                                .AsNoTracking()
                                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                                .Select(ub => ub.BranchId)
                                .ToListAsync();
                             
                             if (!allowedBranchIds.Contains(product.BranchId ?? 0))
                             {
                                 response.AddError("Bu ürünü görme yetkiniz yok.");
                                 return response;
                             }
                             
                             if (currentBranchId > 0 && product.BranchId != currentBranchId)
                             {
                                 response.AddError("Ürün seçili şubeye ait değil.");
                                 return response;
                             }
                        }
                    }
                    var mappedCat = _mapper.Map<ProductUpsertDto>(product);
                    if (mappedCat != null)
                    {
                        response.Result = mappedCat;
                    }
                    else
                        response.AddError("Ürün Bulunamadı");
                // }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetProductById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<ProductUpsertDto>> GetProductByBarcode(string barcode){
            IActionResult<ProductUpsertDto> response = new IActionResult<ProductUpsertDto>{Result = new()};
            try{
                var product = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Barcode.Trim() == barcode.Trim());
                var mappedCat = _mapper.Map<ProductUpsertDto>(product);
                if(mappedCat != null){
                    response.Result = mappedCat;
                } else
                    response.AddError("Ürün Bulunamadı");
                return response;
            } catch(Exception ex){
                _logger.LogError("GetProductForParentSelectList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<string>> UpsertProductImport(ProductTransaction model){
            IActionResult<string> response = new IActionResult<string>{Result = ""};
            try{
                var textInfo = new CultureInfo("tr-TR", false).TextInfo;
                var productName = textInfo.ToTitleCase(model.Product);
                var checkProduct = await _context.DbContext.Product.FirstOrDefaultAsync(x => x.Barcode.Contains(model.Barcode));
                if(checkProduct == null){
                    var productNew = new Product{
                        Barcode = model.Barcode,
                        TaxId = await GetTaxId(model.Tax.HasValue == false ? 6 : model.Tax.Value),
                        Name = productName,
                        Description = productName,
                        ShortName = productName,
                        CreatedDate = DateTime.Now,
                        CreatedId = 1,
                        BrandId = await GetBrandId(model.Manufacturer),
                        Length = 0,
                        Height = model.Height.HasValue == false ? 0 : model.Height.Value,
                        Weight = model.Weight.HasValue == false ? 0 : model.Weight.Value,
                        Width = model.Width.HasValue == false ? 0 : model.Width.Value,
                        CargoDesi = 0,
                        IsCustomerCreated = false,
                        RetailPrice = 0,
                        IsGift = false,
                        Status = 1,
                        CartMinValue = 1,
                        CartMaxValue = 1,
                        IsNewsProduct = false,
                        Price = 0,
                        BranchId = _tenantProvider.GetCurrentBranchId()
                    };
                    await _context.DbContext.Product.AddAsync(productNew);
                    response.Result = "insert";
                } else{
                    var brandId = await GetBrandId(model.Manufacturer);
                    var taxId = await GetTaxId(model.Tax.HasValue == false ? 6 : model.Tax.Value);
                    await _context.DbContext.Product
                        .Where(x => x.Barcode.Contains(model.Barcode) && x.BranchId == _tenantProvider.GetCurrentBranchId())
                        .ExecuteUpdateAsync(x => x.SetProperty(x => x.Name, productName)
                            .SetProperty(x => x.Description, productName)
                            .SetProperty(x => x.ShortName, productName)
                            .SetProperty(x => x.Height, model.Height.HasValue == false ? 0 : model.Height.Value)
                            .SetProperty(x => x.Width, model.Width.HasValue == false ? 0 : model.Width.Value)
                            .SetProperty(x => x.Weight, model.Weight.HasValue == false ? 0 : model.Weight.Value)
                            .SetProperty(x => x.BrandId, brandId)
                            .SetProperty(x => x.TaxId, taxId)
                            .SetProperty(x => x.Status, 1));
                    response.Result = "update";
                }
                var entity = await _context.DbContext.ProductTransaction.AsTracking().FirstOrDefaultAsync(x => x.Barcode == model.Barcode);
                if(entity == null){
                    model.Status = 1;
                    model.CreatedId = 1;
                    model.CreatedDate = DateTime.Now;
                    await _context.DbContext.ProductTransaction.AddAsync(model);
                    response.Result = "insert";
                } else{
                    entity.Product = model.Product;
                    entity.Category = model.Category;
                    entity.SubCategory1 = model.SubCategory1;
                    entity.SubCategory2 = model.SubCategory2;
                    entity.Manufacturer = model.Manufacturer;
                    entity.Form = model.Form ?? "Yok";
                    entity.Tax = model.Tax;
                    entity.Length = model.Length ?? 0;
                    entity.Height = model.Height ?? 0;
                    entity.Width = model.Width ?? 0;
                    entity.ReatilPrice = model.ReatilPrice;
                    entity.Status = 1;
                    entity.ModifiedId = 1;
                    entity.ModifiedDate = DateTime.Now;
                    _context.DbContext.ProductTransaction.Update(entity);
                    response.Result = "update";
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    await _context.SaveChangesAsync();
                    response.AddSuccess("Successfull");
                    return response;
                } else{
                    if(lastResult != null && lastResult.Exception != null){
                        response.Result = lastResult.Exception.Message;
                        response.AddError($"{lastResult.Exception.Message}");
                        _logger.LogError($"UpsertProductImport Exception {lastResult.Exception}");
                    }
                    return response;
                }
            } catch(Exception ex){
                response.Result = ex.Message;
                _logger.LogError($"UpsertProductImport Exception {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<Paging<List<ProductTransactionDto>>>> GetProductsImportList(PageSetting pager){
            var response = OperationResult.CreateResult<Paging<List<ProductTransactionDto>>>();
            try{
                var rs = await _productTransactionRepository.GetAll(predicate:x => x.Status == 1).ToPagedResultAsync<ProductTransactionDto>(pager, _mapper);
                response.Result = rs;
            } catch(Exception e){
                _logger.LogError("GetProductsImportList Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<bool>> UpsertProductLastCheckImport(){
            var response = new IActionResult<bool>{Result = new bool()};
            var productData = await _context.DbContext.ProductTransaction.AsTracking().ToListAsync();
            foreach(var item in productData){
                if(item.Barcode != null){
                    var parameter = new{barcode = item.Barcode};
                    await _reportService.ExecuteRunner("fn_productTransaction", parameter);
                } else{
                    response.Result = false;
                }
            }
            response.Result = true;
            return response;
        }
        public async Task<List<DuplicateProductListDto>> GetDublicateProductList(){
            var response = OperationResult.CreateResult<List<DuplicateProductListDto>>();
            try{
                var parameter = new{};
                var rs = await _reportService.Execute<DuplicateProductListDto>("fn_report_duplicate_product", parameter);
                response.Result = rs;
            } catch(Exception e){
                _logger.LogError("GetProducts Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response.Result;
        }
        public async Task<IActionResult<Paging<List<ProductListDto>>>> GetProducts(PageSetting pager){
            var response = OperationResult.CreateResult<Paging<List<ProductListDto>>>();
            try{
                // Capture context BEFORE scope
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var context = _context;
                    var repository = context.GetRepository<Product>();
                    
                    List<int> allowedBranchIds = new();
                    if (!isGlobalAdmin && userId > 0)
                    {
                         allowedBranchIds = context.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }

                    var query = repository.GetAll(predicate: null, disableTracking: true, ignoreQueryFilters: true)
                        .Include(x => x.Tax)
                        .Include(c => c.Brand)
                        .Include(p => p.ProductImage)
                        .Include(p => p.Categories)
                        .Include(p => p.ProductUnits)
                        .Where(s => s.Status != (int) EntityStatus.Deleted
                             && (
                                 !isGlobalAdmin ? 
                                 (
                                     (!s.BranchId.HasValue || s.BranchId == 0) || // Global Items
                                     allowedBranchIds.Contains(s.BranchId.Value) // User's Allowed Branches
                                 ) : true // Global Admin sees all
                            )
                             && (
                                 currentBranchId > 0 ? 
                                 (!s.BranchId.HasValue || s.BranchId == 0 || s.BranchId == currentBranchId) // Current Branch context filters
                                 : true
                             ));

                    query = query.ApplySmartSearch(pager.Search);

                    var res = await query.AsSplitQuery()
                        .ToPagedResultAsync<ProductListForProjectionDto>(pager, _mapper);
                    var mappedRes = _mapper.Map<List<ProductListDto>>(res.Data);
                    Paging<List<ProductListDto>> pagingResult = new Paging<List<ProductListDto>>{Data = mappedRes, DataCount = res.DataCount};
                    response.Result = pagingResult;
                // }
            } catch(Exception e){
                _logger.LogError("GetProducts Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }

        public async Task<IActionResult<List<ProductListDto>>> SearchProducts(string search)
        {
            var response = OperationResult.CreateResult<List<ProductListDto>>();
            try
            {
                if (string.IsNullOrWhiteSpace(search))
                {
                    response.Result = new List<ProductListDto>();
                    return response;
                }

                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var context = _context;
                    var repository = context.GetRepository<Product>();
                    
                    List<int> allowedBranchIds = new();
                    if (!isGlobalAdmin && userId > 0)
                    {
                         allowedBranchIds = context.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }

                    var query = repository.GetAll(predicate: null, disableTracking: true, ignoreQueryFilters: true)
                        .AsNoTracking()
                        .Where(x => x.Status != (int)EntityStatus.Deleted
                             && (
                                 !isGlobalAdmin ? 
                                 (
                                     (!x.BranchId.HasValue || x.BranchId == 0) || 
                                     allowedBranchIds.Contains(x.BranchId.Value)
                                 ) : true
                            )
                             && (
                                 currentBranchId > 0 ? 
                                 (!x.BranchId.HasValue || x.BranchId == 0 || x.BranchId == currentBranchId)
                                 : true
                             ))
                        .ApplySmartSearch(search);

                var products = await query
                    .OrderByDescending(x => x.Id)
                    .Take(50)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Barcode = p.Barcode,
                        Price = p.Price,
                        RetailPrice = p.RetailPrice,
                        BrandId = p.BrandId,
                        Brand = p.Brand != null ? new Brand { Name = p.Brand.Name } : null,
                        Kdv = p.Tax != null ? p.Tax.TaxRate : (int?)null
                    })
                    .ToListAsync();
                
                response.Result = products;
                // }
            }
            catch (Exception e)
            {
                _logger.LogError("SearchProducts Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<List<ProductListDto>>> GetProducts(){
            IActionResult<List<ProductListDto>> response = new(){Result = new()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                List<int> allowedBranchIds = new();
                if (!isGlobalAdmin && userId > 0)
                {
                        allowedBranchIds = _context.DbContext.UserBranches
                        .AsNoTracking()
                        .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                        .Select(ub => ub.BranchId)
                        .ToList();
                }

                var products = await _repository.GetAllAsync(
                    predicate: x => x.Status != (int)EntityStatus.Deleted
                        && (
                                !isGlobalAdmin ? 
                                 (
                                     (!x.BranchId.HasValue || x.BranchId == 0) || 
                                     allowedBranchIds.Contains(x.BranchId.Value)
                                 ) : true
                        )
                         && (
                             currentBranchId > 0 ? 
                             (!x.BranchId.HasValue || x.BranchId == 0 || x.BranchId == currentBranchId)
                             : true
                         ),
                    ignoreQueryFilters: true);
                var mappedEntites = _mapper.Map<List<ProductListDto>>(products);
                if(mappedEntites == null) return response;
                if(mappedEntites.Count > 0) response.Result = mappedEntites;
                return response;
            } catch(Exception ex){
                _logger.LogError("GetProducts Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<ProductListDto>>> GetProducts(List<int> Ids){
            IActionResult<List<ProductListDto>> response = new(){Result = new()};
            try{
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;
                int userId = 0;
                if (user != null) int.TryParse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

                List<int> allowedBranchIds = new();
                if (!isGlobalAdmin && userId > 0)
                {
                        allowedBranchIds = _context.DbContext.UserBranches
                        .AsNoTracking()
                        .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                        .Select(ub => ub.BranchId)
                        .ToList();
                }

                var products = await _repository.GetAllAsync(
                    predicate: x => Ids.Contains(x.Id)
                        && (
                                isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) :
                                (allowedBranchIds.Contains(x.BranchId ?? 0) && (currentBranchId == 0 || x.BranchId == currentBranchId))
                        ),
                    ignoreQueryFilters: true);
                var mappedEntites = _mapper.Map<List<ProductListDto>>(products);
                if(mappedEntites != null){
                    if(mappedEntites.Count > 0) response.Result = mappedEntites;
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetProducts Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<bool>> GetDuplicateProductDelete(DuplicateProductDeleteDto model){
            var response = new IActionResult<bool>();
            try{
                var products = await _context.DbContext.Product.Where(x => (x.Name ?? "").ToLower() == (model.Name ?? "").ToLower()).ToListAsync();
                foreach(var product in products){
                    var checkSellerItemId = await _context.DbContext.ProductSellerItems.FirstOrDefaultAsync(x => x.ProductId == product.Id && x.Status == 1);
                    if(checkSellerItemId == null){
                        _context.DbContext.Product.Remove(product);
                        await _context.SaveChangesAsync();
                        response.AddSuccess($"{product.Id} Ürün silindi");
                        response.Result = true;
                    } else{
                        response.AddSuccess($"{product.Id} Nolu ürünün ilanı var silinemez");
                        response.Result = false;
                    }
                }
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    response.Result = true;
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetDuplicateProductDelete Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<ProductAdvertListDto>>> GetProductAdvertListById(int productId){
            IActionResult<List<ProductAdvertListDto>> response = new(){Result = new()};
            try{
                
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var dbContext = _context.DbContext;
                    var advertList = await dbContext.ProductSellerItems
                        .AsNoTracking()
                        .Include(x => x.Company)
                        .Where(x => x.ProductId == productId)
                        .ToListAsync();
                    if(advertList != null){
                        response.Result = new List<ProductAdvertListDto>(advertList.Select(s => new ProductAdvertListDto{
                                     AccountName = s.Company.AccountName == null ? s.Company.FirstName + " " + s.Company.LastName : s.Company.AccountName,
                                     Price = s.Price,
                                     Stock = s.Stock,
                                     ExprationDate = s.ExprationDate,
                                     Status = s.Status == 1 ? "Aktif" : "Pasif"
                                 }
                             )
                         );
                         return response;
                    }
                // }
            } catch(Exception ex){
                _logger.LogError("GetProductAdvertListById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
            return response;
        }
        public async Task<IActionResult<List<ProductSellerItemListDto>>> GetSellerItemsByProduct(int productId)
        {
            var response = new IActionResult<List<ProductSellerItemListDto>> { Result = new List<ProductSellerItemListDto>() };
            try
            {
                // Ayrı scope kullan: aynı request içinde UpsertProduct ile aynı DbContext paylaşılmasın (Npgsql "command already in progress" hatasını önler)
                using var scope = _serviceScopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>().DbContext;

                var query = dbContext.SellerItems
                    .AsNoTracking()
                    .Include(x => x.Seller)
                        .ThenInclude(s => s.City)
                    .Where(x => x.ProductId == productId && x.Status == (int)EntityStatus.Active)
                    .OrderByDescending(x => x.Id)
                    .Take(50);

                var items = await query.ToListAsync();

                response.Result = items.Select(s => new ProductSellerItemListDto
                {
                    Id = s.Id,
                    SellerName = s.Seller.Name,
                    SellerEmail = s.Seller.Email,
                    SellerCity = s.Seller.City != null ? s.Seller.City.Name : null,
                    Stock = s.Stock,
                    CostPrice = s.CostPrice,
                    SalePrice = s.SalePrice,
                    Commission = s.Seller.Commission,
                    Currency = s.Currency,
                    Unit = s.Unit
                }).ToList();

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSellerItemsByProduct Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<Paging<List<ProductOnlineDto>>>> GetProductOnline(PageSetting pager){
            var response = OperationResult.CreateResult<Paging<List<ProductOnlineDto>>>();
            try{
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repositoryOnline = context.GetRepository<ProductOnline>();
                    
                    var productOnlines = await repositoryOnline.GetAll(predicate:null, disableTracking:true).ToPagedResultAsync<ProductOnlineDto>(pager, _mapper);
                    var mappedRes = _mapper.Map<List<ProductOnlineDto>>(productOnlines.Data);
                    var pagingResult = new Paging<List<ProductOnlineDto>>{Data = mappedRes, DataCount = productOnlines.DataCount};
                    response.Result = pagingResult;
                }
            } catch(Exception e){
                _logger.LogError("GetProductOnline Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<List<ProductDublicaListDto>>> GetDublicateProductListWitGroup(){
            IActionResult<List<ProductDublicaListDto>> response = OperationResult.CreateResult<List<ProductDublicaListDto>>();
            try{
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    var products = await dbContext.Product.AsNoTracking().ToListAsync();
                    var barcodeDictionary = new Dictionary<string, List<Product>>();
                    var nameDictionary = new Dictionary<string, List<Product>>();
                    foreach(var product in products){
                        if(string.IsNullOrEmpty(product.Barcode)) continue;
                        var barcodes = product.Barcode.Split(',').Select(b => b.Trim());
                        foreach(var barcode in barcodes){
                            if(string.IsNullOrEmpty(barcode)) continue;
                            if(!barcodeDictionary.ContainsKey(barcode)){
                                barcodeDictionary[barcode] = new List<Product>();
                            }
                            barcodeDictionary[barcode].Add(product);
                        }
                    }
                    foreach(var product in products){
                        if(string.IsNullOrEmpty(product.Name)) continue;
                        if(!nameDictionary.ContainsKey(product.Name)){
                            nameDictionary[product.Name] = new List<Product>();
                        }
                        nameDictionary[product.Name].Add(product);
                    }
                    var productsWithSameBarcode = barcodeDictionary.Where(kvp => kvp.Value.Count > 1).SelectMany(kvp => kvp.Value).ToList();
                    var productsWithSameName = nameDictionary.Where(kvp => kvp.Value.Count > 1).SelectMany(kvp => kvp.Value).ToList();
                    var combinedProducts = productsWithSameBarcode.Concat(productsWithSameName).DistinctBy(x => x.Id).Select(product => new ProductDublicaListDto{Id = product.Id, Name = product.Name ?? "", Barcode = product.Barcode, DisplayName = $"{product.Id} - {product.Name ?? ""} - {product.Barcode}"}).ToList();
                    response.Result = combinedProducts;
                }
            } catch(Exception e){
                _logger.LogError("GetDublicateProductListWitGroup Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<bool>> MergeProductsAsync(MergeProductUpsertDto model){
            var response = new IActionResult<bool>{Result = true};
            try{
                if(model.OldProductId == 0 || model.NewProductId == 0 || model.OldProductId == model.NewProductId){
                    response.AddSystemError("İki ürün seçilmemiş veya aynı ürün seçilmişse işlem yapılmaz");
                    return response;
                }
                var oldProduct = await _context.DbContext.Product.FindAsync(model.OldProductId);
                var newProduct = await _context.DbContext.Product.FindAsync(model.NewProductId);
                if(oldProduct != null && newProduct != null){
                    var oldBarcodes = (oldProduct.Barcode ?? "").Split(',').Select(b => b.Trim()).ToList();
                    var newBarcodes = (newProduct.Barcode ?? "").Split(',').Select(b => b.Trim()).ToList();
                    foreach (var newBarcode in newBarcodes.Where(newBarcode => !oldBarcodes.Contains(newBarcode)))
                    {
                        oldBarcodes.Add(newBarcode);
                    }

                    oldProduct.Barcode = string.Join(",", oldBarcodes);
                    var productSellerItems = await _context.DbContext.ProductSellerItems.Where(ps => ps.ProductId == newProduct.Id).ToListAsync();
                    foreach(var item in productSellerItems){
                        item.ProductId = oldProduct.Id;
                    }
                    _context.DbContext.Product.Remove(newProduct);
                    await _context.SaveChangesAsync();
                    var lastResult = _context.LastSaveChangesResult;
                    if(lastResult.IsOk){
                        response.AddSuccess("Barkodlar birleştirildi ve ikinci ürün silindi.");
                    } else{
                        response.AddError(lastResult.Exception!.Message);
                    }
                    return response;
                }
            } catch(Exception ex){
                _logger.LogError($"UpsertCategory Exception {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
            return response;
        }
        public async Task<IActionResult<int>> UpsertProduct(AuditWrapDto<ProductUpsertDto> model){
            var response = new IActionResult<int>{Result = 0};
            try{
                var dto = model.Dto;
                int productIdLocal = 0;
                int productStatusLocal = dto.StatusBool == true ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                int? brandIdLocal = null;
                var branchId = _tenantProvider.GetCurrentBranchId();
                if(!dto.Id.HasValue){
                    // Check for duplicate name in current branch
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(p => p.Name.ToLower() == (dto.Name ?? "").ToLower() && p.BranchId == branchId && p.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli ürün bu şubede zaten mevcut.");
                        return response;
                    }

                    var entity = _mapper.Map<Product>(dto);
                    entity.Status = dto.StatusBool == true ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    if(model.Dto.Width > 0 && model.Dto.Length > 0 && model.Dto.Height > 0){
                        entity.CargoDesi = (decimal) ((model.Dto.Width * model.Dto.Height * model.Dto.Height) / 3000);
                    }
                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        entity.BranchId = branchId;
                    }
                    await _repository.InsertAsync(entity);
                    productIdLocal = entity.Id;
                    productStatusLocal = entity.Status;
                    brandIdLocal = entity.BrandId;
                } else{
                    var current = await _repository.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: true);
                    if(current == null){
                        response.AddError("Ürün bulunamadı");
                        return response;
                    }

                    // Check for duplicate name in same branch (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(p => p.Id != dto.Id && p.Name.ToLower() == (dto.Name ?? "").ToLower() && p.BranchId == current!.BranchId && p.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        response.AddError($"'{dto.Name}' isimli ürün bu şubede zaten mevcut.");
                        return response;
                    }

                    var updated = _mapper.Map<Product>(dto);
                    updated.Id = current.Id;
                    updated.CreatedId = current.CreatedId;
                    updated.CreatedDate = current.CreatedDate;
                    updated.Status = dto.StatusBool == true ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                    updated.ModifiedId = model.UserId;
                    updated.ModifiedDate = DateTime.Now;
                    updated.IsCustomerCreated = false;
                    // Vergi oranı (KDV) her zaman DTO'dan yazılsın; mapper bazen atlayabiliyor
                    updated.TaxId = dto.TaxId ?? current.TaxId;
                    if(dto.Width.HasValue && dto.Length.HasValue && dto.Height.HasValue && dto.Width.Value > 0 && dto.Length.Value > 0 && dto.Height.Value > 0){
                        updated.CargoDesi = (decimal) ((dto.Width.Value * dto.Length.Value * dto.Height.Value) / 3000);
                    }
                    if (_tenantProvider.IsMultiTenantEnabled)
                    {
                        updated.BranchId = current.BranchId; // Preserve branch
                    }
                    _repository.AttachAsModified(updated, excludeNavigations: true);
                    // Vergi oranının UPDATE'te kesin yazılması için property'yi Modified işaretle
                    _context.DbContext.Entry(updated).Property(nameof(Product.TaxId)).IsModified = true;
                    productIdLocal = updated.Id;
                    productStatusLocal = updated.Status;
                    brandIdLocal = updated.BrandId;
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    var targetProductId = dto.Id ?? productIdLocal;

                    // Handle Category Saving
                    if (dto.CategoryIds != null)
                    {
                        var productCategoryRepo = _context.GetRepository<ProductCategories>();
                        var existingCategories = await productCategoryRepo.GetAllAsync(predicate: x => x.ProductId == targetProductId);
                        var existingCategoryIds = existingCategories.Select(x => x.CategoryId).ToList();

                        // To added
                        var toAdd = dto.CategoryIds.Where(x => !existingCategoryIds.Contains(x)).ToList();
                        foreach (var catId in toAdd)
                        {
                            await productCategoryRepo.InsertAsync(new ProductCategories
                            {
                                ProductId = targetProductId,
                                CategoryId = catId
                            });
                        }

                        // To delete
                        var toDelete = existingCategories.Where(x => !dto.CategoryIds.Contains(x.CategoryId)).ToList();
                        foreach (var item in toDelete)
                        {
                             _context.DbContext.ProductCategories.Remove(item);
                        }
                    }

                    // Handle Unit Saving
                    if (dto.UnitId.HasValue)
                    {
                        var productUnitRepo = _context.GetRepository<ProductUnit>();
                        var existingUnits = await productUnitRepo.GetAllAsync(predicate: x => x.ProductId == targetProductId);
                        
                        // Sadece varsayılan birimi ekle veya güncelle
                        var existingDefaultUnit = existingUnits.FirstOrDefault(x => x.IsDefault);
                        
                        if (existingDefaultUnit == null)
                        {
                            // Yeni varsayılan birim ekle
                            await productUnitRepo.InsertAsync(new ProductUnit
                            {
                                ProductId = targetProductId,
                                UnitId = dto.UnitId.Value,
                                UnitValue = 1, // Varsayılan değer
                                IsDefault = true
                            });
                        }
                        else if (existingDefaultUnit.UnitId != dto.UnitId.Value)
                        {
                            // Varolan varsayılan birimi güncelle
                            existingDefaultUnit.UnitId = dto.UnitId.Value;
                            existingDefaultUnit.UnitValue = 1;
                            existingDefaultUnit.ModifiedDate = DateTime.Now;
                            existingDefaultUnit.ModifiedId = model.UserId;
                            _context.DbContext.ProductUnits.Update(existingDefaultUnit);
                        }
                    }

                    var productSellerList = await _context.DbContext.ProductSellerItems
                        .Where(x => x.ProductId == targetProductId)
                        .ToListAsync();
                    foreach (var ps in productSellerList)
                    {
                        if (productStatusLocal == 0)
                        {
                            ps.Status = 0;
                        }
                        else
                            if (productStatusLocal == 1 && ps.Status == 1)
                        {
                            ps.Status = 1;
                        }
                        else
                                if (productStatusLocal == 1 && ps.Status == 99)
                        {
                            ps.Status = 99;
                        }
                        else
                                    if (productStatusLocal == 1 && ps.Status == 0)
                        {
                            ps.Status = 1;
                        }
                        ps.BrandId = brandIdLocal;
                        _context.DbContext.ProductSellerItems.Update(ps);
                    }
                    await _context.SaveChangesAsync();
                    response.Result = targetProductId;
                    response.AddSuccess("Successfull");
                    return response;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            response.AddError($"'{dto.Name}' isimli ürün zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                        }
                        else
                        {
                            response.AddError("Herhangi bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.");
                            _logger.LogError($"UpsertProduct SaveChanges Exception {lastResult.Exception.ToString()}");
                        }
                    }
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"UpsertProduct Exception {ex.ToString()}");
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    response.AddError($"'{model.Dto.Name}' isimli ürün zaten mevcut (Genel bir kısıtlama nedeniyle bu isim başka bir şubede de kullanılamıyor olabilir).");
                }
                else
                {
                    response.AddSystemError(ex.ToString());
                }
                return response;
            }
        }
        public async Task<int> GetTaxId(int taxValue){
            var returnId = 0;
            try{
                var tax = await _context.DbContext.Tax.FirstOrDefaultAsync(x => x.TaxRate == taxValue);
                returnId = tax?.Id ?? 6;
            } catch(Exception e){
                Console.WriteLine(e);
                throw;
            }
            return returnId;
        }
        public async Task<int> GetBrandId(string brandValue){
            var returnId = 0;
            try{
                var brand = await _context.DbContext.Brand.FirstOrDefaultAsync(x => x.Name.ToLower().Contains(brandValue.ToLower()));
                returnId = brand?.Id ?? 111200;
            } catch(Exception e){
                Console.WriteLine(e);
                throw;
            }
            return returnId;
        }
        public async Task<IActionResult<List<ProductCompatibleVehicleDto>>> GetCompatibleVehicles(int productId)
        {
            var response = new IActionResult<List<ProductCompatibleVehicleDto>> { Result = new() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();

                    var groupCodes = await context.DbContext.ProductGroupCodes
                        .Where(x => x.ProductId == productId)
                        .Select(x => x.OemCode)
                        .ToListAsync();

                    if (!groupCodes.Any()) return response;

                    var partNumbers = groupCodes
                        .Where(gc => !string.IsNullOrEmpty(gc))
                        .SelectMany(gc => gc.Split('|', StringSplitOptions.RemoveEmptyEntries))
                        .Select(p => p.Trim())
                        .Distinct()
                        .ToList();

                    if (!partNumbers.Any()) return response;

                    var dotParts = await context.DbContext.DotParts
                        .Where(x => partNumbers.Contains(x.PartNumber))
                        .ToListAsync();

                    foreach (var part in dotParts)
                    {
                        if (!string.IsNullOrEmpty(part.SubModelsJson))
                        {
                            try
                            {
                                var subModels = System.Text.Json.JsonSerializer.Deserialize<List<SubModelHelper>>(part.SubModelsJson);
                                if (subModels != null)
                                {
                                    foreach (var sm in subModels)
                                    {
                                        response.Result.Add(new ProductCompatibleVehicleDto
                                        {
                                            Manufacturer = part.ManufacturerName ?? "Unknown",
                                            Model = part.BaseModelName ?? "Unknown",
                                            SubModel = sm.Name ?? "Unknown",
                                            PartNumber = part.PartNumber
                                        });
                                    }
                                }
                            }
                            catch
                            {
                                response.Result.Add(new ProductCompatibleVehicleDto
                                {
                                    Manufacturer = part.ManufacturerName ?? "Unknown",
                                    Model = part.BaseModelName ?? "Unknown",
                                    SubModel = "-",
                                    PartNumber = part.PartNumber
                                });
                            }
                        }
                        else
                        {
                            response.Result.Add(new ProductCompatibleVehicleDto
                            {
                                Manufacturer = part.ManufacturerName ?? "Unknown",
                                Model = part.BaseModelName ?? "Unknown",
                                SubModel = "-",
                                PartNumber = part.PartNumber
                            });
                        }
                    }

                    response.Result = response.Result
                        .GroupBy(x => new { x.Manufacturer, x.Model, x.SubModel, x.PartNumber })
                        .Select(g => g.First())
                        .OrderBy(x => x.Manufacturer)
                        .ThenBy(x => x.Model)
                        .ToList();

                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCompatibleVehicles Exception: {ex}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        private class SubModelHelper { public string Name { get; set; } = ""; }
    }
}
