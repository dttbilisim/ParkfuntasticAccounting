using AutoMapper;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Dtos.UserAddressDto;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Dynamic.Core;
using ecommerce.Admin.EFCore.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Interfaces; // Add this
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;

namespace ecommerce.Admin.Services.Concreate
{
    public class CustomerService : ICustomerService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITenantProvider _tenantProvider;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "customers";

        public CustomerService(IUnitOfWork<ApplicationDbContext> uow, IMapper mapper, ILogger<CustomerService> logger, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor, ITenantProvider tenantProvider, ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter, ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _uow = uow;
            _permissionService = permissionService;
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _httpContextAccessor = httpContextAccessor;
            _tenantProvider = tenantProvider;
            _roleFilter = roleFilter;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<List<CustomerListDto>>>> GetPagedCustomers(PageSetting pager)
        {
            var response = new IActionResult<Paging<List<CustomerListDto>>> { Result = new() };
            try
            {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var uow = _uow;
                    var repo = uow.GetRepository<Customer>();
                    
                    if (!await CanView())
                    {
                         response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                         return response;
                    }

                    var query = repo.GetAll(disableTracking: true, ignoreQueryFilters: true)
                                    .Include(x => x.Corporation)
                                    .Include(x => x.City)
                                    .Include(x => x.Town)
                                    .Include(x => x.Region)
                                    .Include(x => x.CustomerBranches)
                                    .AsQueryable();

                    // Role-based filtering - clean ve maintainable
                    query = _roleFilter.ApplyFilter(query, uow.DbContext);

                    // Filter
                    if (!string.IsNullOrEmpty(pager.Filter))
                    {
                        query = query.Where(pager.Filter);
                    }

                    if (!string.IsNullOrEmpty(pager.Search))
                    {
                        var searchPattern = $"%{pager.Search.Trim()}%";
                        query = query.Where(x => EF.Functions.ILike(x.Name, searchPattern) || EF.Functions.ILike(x.Code, searchPattern));
                    }

                    // Count
                    var count = await query.CountAsync();
                    response.Result.DataCount = count;

                    // Order
                     if (!string.IsNullOrEmpty(pager.OrderBy))
                    {
                         query = query.OrderBy(pager.OrderBy);
                    }
                    else
                    {
                        query = query.OrderBy(x => x.Id);
                    }

                    // Paging
                    if (pager.Skip.HasValue) query = query.Skip(pager.Skip.Value);
                    if (pager.Take.HasValue) query = query.Take(pager.Take.Value);

                    var customers = (await query.ToListAsync()).DistinctBy(c => c.Id).ToList();
                    response.Result.Data = _mapper.Map<List<CustomerListDto>>(customers);
                // }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedCustomers error");
                response.AddSystemError(ex.Message);
                return response;
            }
        }

        /// <summary>
        /// Cari listesini yetki kontrolü olmadan, sadece BranchId filtreleme ile döndürür.
        /// Fatura oluşturma modal'ından çağrılır.
        /// Global query filter bypass edilir, doğrudan BranchId ile filtrelenir.
        /// </summary>
        public async Task<IActionResult<Paging<List<CustomerListDto>>>> GetPagedCustomersForInvoice(PageSetting pager)
        {
            var response = new IActionResult<Paging<List<CustomerListDto>>> { Result = new() };
            try
            {
                // Yeni scope kullan: render hatası veya uzun süren istek sonrası inject edilen DbContext dispose edilmiş olabilir.
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Customer>();

                    // Yetki kontrolü yok (CanView bypass), global query filter bypass
                    var query = repo.GetAll(disableTracking: true, ignoreQueryFilters: true)
                                    .Include(x => x.Corporation)
                                    .Include(x => x.City)
                                    .Include(x => x.Town)
                                    .Include(x => x.Region)
                                    .Include(x => x.CustomerBranches)
                                    .AsQueryable();

                    // Doğrudan BranchId filtreleme
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    if (currentBranchId > 0)
                    {
                        query = query.Where(x => !x.BranchId.HasValue || x.BranchId == currentBranchId || x.BranchId == 0);
                    }
                    else if (!_tenantProvider.IsGlobalAdmin)
                    {
                        var allowedBranchIds = await _roleFilter.GetAllowedBranchIdsAsync(uow.DbContext);
                        if (allowedBranchIds.Any())
                        {
                            query = query.Where(x => !x.BranchId.HasValue || allowedBranchIds.Contains(x.BranchId.Value) || x.BranchId == 0);
                        }
                        else
                        {
                            query = query.Where(x => !x.BranchId.HasValue || x.BranchId == 0);
                        }
                    }

                    if (!string.IsNullOrEmpty(pager.Filter))
                    {
                        query = query.Where(pager.Filter);
                    }

                    if (!string.IsNullOrEmpty(pager.Search))
                    {
                        var searchPattern = $"%{pager.Search.Trim()}%";
                        query = query.Where(x => EF.Functions.ILike(x.Name, searchPattern) || EF.Functions.ILike(x.Code, searchPattern));
                    }

                    var count = await query.CountAsync();
                    response.Result.DataCount = count;

                    if (!string.IsNullOrEmpty(pager.OrderBy))
                    {
                        query = query.OrderBy(pager.OrderBy);
                    }
                    else
                    {
                        query = query.OrderBy(x => x.Id);
                    }

                    if (pager.Skip.HasValue) query = query.Skip(pager.Skip.Value);
                    if (pager.Take.HasValue) query = query.Take(pager.Take.Value);

                    var customers = (await query.ToListAsync()).DistinctBy(c => c.Id).ToList();
                    response.Result.Data = _mapper.Map<List<CustomerListDto>>(customers);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedCustomersForInvoice error");
                response.AddSystemError(ex.Message);
                return response;
            }
        }

        public async Task<IActionResult<CustomerUpsertDto>> GetCustomerById(int id)
        {
            var rs = new IActionResult<CustomerUpsertDto> { Result = new() };
            try
            {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var uow = _uow;
                    var repo = uow.GetRepository<Customer>();
                    
                    // Bypass permission check for Plasiyer and B2B users who need to load customer context
                    if (!await CanView() && !_tenantProvider.IsPlasiyer && !_tenantProvider.IsCustomerB2B)
                    {
                        rs.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                        return rs;
                    }

                    var query = repo.GetAll(
                        predicate: x => x.Id == id,
                        include: x => x.Include(i => i.CustomerBranches).ThenInclude(i => i.Branch).ThenInclude(i => i.Corporation)
                                        .Include(i => i.City)
                                        .Include(i => i.Town),
                        ignoreQueryFilters: true
                    );
                    
                    query = _roleFilter.ApplyFilter(query, uow.DbContext);

                    var entity = await query.FirstOrDefaultAsync();
                    
                    if (entity == null)
                    {
                        // Check existence for security msg
                        var exists = await repo.GetAll(predicate: x => x.Id == id, ignoreQueryFilters: true).AnyAsync();
                        if (exists)
                        {
                             rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                             return rs;
                        }
                        rs.AddError("Cari bulunamadı");
                        return rs;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, uow.DbContext))
                    {
                         rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return rs;
                    }

                    rs.Result = _mapper.Map<CustomerUpsertDto>(entity);
                    // Şehir ve ilçe adlarını doldur (e-fatura için gerekli)
                    rs.Result.CityName = entity.City?.Name;
                    rs.Result.TownName = entity.Town?.Name;
                // }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerById error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCustomer(CustomerUpsertDto dto)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var uow = _uow;
                    var repo = uow.GetRepository<Customer>();
                    Customer entity;

                    // New Record Check
                    if (!dto.Id.HasValue || dto.Id == 0)
                    {
                        if (!await CanCreate())
                        {
                            rs.AddError("Ekleme yetkiniz bulunmamaktadır.");
                            return rs;
                        }
                    }
                    else
                    {
                        if (!await CanEdit())
                        {
                            rs.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                            return rs;
                        }

                         // Check Access for Update
                         var existingQuery = repo.GetAll(predicate: x => x.Id == dto.Id, ignoreQueryFilters: true);
                         existingQuery = _roleFilter.ApplyFilter(existingQuery, uow.DbContext);
                         
                         var existing = await existingQuery.FirstOrDefaultAsync();
                         if (existing == null)
                         {
                             // Check if exists but no access
                             var reallyExists = await repo.GetAll(predicate: x => x.Id == dto.Id, ignoreQueryFilters: true).AnyAsync();
                             if (reallyExists)
                             {
                                 rs.AddError("Bu cariyi güncelleme yetkiniz yok (Şube Yetkisi).");
                                 return rs;
                             }
                             rs.AddError("Cari bulunamadı.");
                             return rs;
                         }

                         if (!await _roleFilter.CanAccessBranchAsync(existing.BranchId ?? 0, uow.DbContext))
                        {
                             rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                             return rs;
                        }
                    }

                    // Yeni cari: Id null veya 0 ise CorporationId atanmamışsa tenant'tan al
                    var isNewCustomer = !dto.Id.HasValue || dto.Id == 0;
                    if (isNewCustomer && dto.CorporationId == 0)
                    {
                        dto.CorporationId = _tenantProvider.GetCurrentCorporationId();
                    }
                    if (isNewCustomer && dto.CorporationId == 0)
                    {
                        rs.AddError("Cari şirket bilgisi alınamadı. Lütfen giriş yaptığınız şirket/şube ile devam edin.");
                        return rs;
                    }
                    var mainBranchId = _tenantProvider.GetCurrentBranchId();

                    if (dto.Id.HasValue && dto.Id > 0)
                    {
                        // Update
                        entity = await repo.GetFirstOrDefaultAsync(
                            predicate: x => x.Id == dto.Id, 
                            include: x => x.Include(i => i.CustomerBranches),
                            disableTracking: false,
                            ignoreQueryFilters: true);
                        
                        if (entity == null)
                        {
                            rs.AddError("Cari bulunamadı");
                            return rs;
                        }
                    
                        
                        _logger.LogInformation($"Entity BEFORE Map - ID: {entity.Id}, RiskLimit: {entity.RiskLimit}, Vade: {entity.Vade}");
                        
                        _mapper.Map(dto, entity);
                        
                        _logger.LogInformation($"Entity AFTER Map - ID: {entity.Id}, RiskLimit: {entity.RiskLimit}, Vade: {entity.Vade}");
                        
                        // Sadece FK kullan; navigation set etme (City.Name NOT NULL hatasını önler)
                        entity.City = null;
                        entity.Town = null;
                        
                        // Update BranchId
                        entity.BranchId = mainBranchId;

                        // Handle branches
                        var existingBranches = entity.CustomerBranches.ToList();
                        var dtoBranchIds = dto.Branches.Select(b => b.BranchId).ToList();

                        // Remove
                        foreach (var eb in existingBranches)
                        {
                            if (!dtoBranchIds.Contains(eb.BranchId))
                            {
                                entity.CustomerBranches.Remove(eb);
                            }
                        }

                        // Add or Update
                        foreach (var db in dto.Branches)
                        {
                            var existing = existingBranches.FirstOrDefault(b => b.BranchId == db.BranchId);
                            if (existing == null)
                            {
                                entity.CustomerBranches.Add(new CustomerBranch
                                {
                                    BranchId = db.BranchId,
                                    IsDefault = db.IsDefault
                                });
                            }
                            else
                            {
                                existing.IsDefault = db.IsDefault;
                            }
                        }

                        // Explicitly call Update to ensure state is set to Modified
                        repo.Update(entity); 
                    }
                    else
                    {
                        // Insert
                        entity = _mapper.Map<Customer>(dto);
                        entity.BranchId = mainBranchId;
                        // AuditableEntity alanları: CreatedDate ve CreatedId zorunlu
                        entity.CreatedDate = DateTime.UtcNow;
                        entity.Status = (int)EntityStatus.Active;
                        var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        entity.CreatedId = int.TryParse(userIdClaim, out var uid) && uid > 0 ? uid : 1;
                        // Sadece CityId/TownId kullan; navigation set etme (City.Name NOT NULL hatasını önler)
                        entity.City = null;
                        entity.Town = null;
                        
                        // Add branches
                        foreach (var db in dto.Branches)
                        {
                            entity.CustomerBranches.Add(new CustomerBranch
                            {
                                BranchId = db.BranchId,
                                IsDefault = db.IsDefault
                            });
                        }

                        await repo.InsertAsync(entity);
                    }

                    var affected = await uow.SaveChangesAsync(); // Use the scoped UoW
                    _logger.LogInformation($"SaveChanges result: {affected} rows affected.");
                    
                    var result = uow.LastSaveChangesResult;

                    if (result.IsOk)
                    {
                        rs.AddSuccess("İşlem başarıyla tamamlandı");
                    }
                    else
                    {
                        _logger.LogError($"SaveChanges Failed Exception: {result.Exception?.Message}");

                        if (result.Exception != null)
                            rs.AddError(result.Exception.Message);
                        else
                            rs.AddError("Kayıt sırasında bir hata oluştu");
                    }
                // }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteCustomer(int id)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var uow = _uow; // Replacement
                    var repo = uow.GetRepository<Customer>();
                     
                     if (!await CanDelete())
                     {
                         rs.AddError("Silme yetkiniz bulunmamaktadır.");
                         return rs;
                     }

                     var query = repo.GetAll(predicate: x => x.Id == id, include: x => x.Include(i => i.CustomerBranches), ignoreQueryFilters: true);
                     query = _roleFilter.ApplyFilter(query, uow.DbContext);
                     
                     var entity = await query.FirstOrDefaultAsync();
                     
                     if (entity == null)
                     {
                         // Check existence
                         var exists = await repo.GetAll(predicate: x => x.Id == id, ignoreQueryFilters: true).AnyAsync();
                         if (exists)
                         {
                             rs.AddError("Bu cariyi silme yetkiniz yok (Şube Yetkisi).");
                             return rs;
                         }
                         rs.AddError("Cari bulunamadı");
                         return rs;
                     }
                     
                     if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, uow.DbContext))
                     {
                          rs.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                          return rs;
                     }

                     repo.Delete(entity);
                     await uow.SaveChangesAsync();
                     
                     if (uow.LastSaveChangesResult.IsOk)
                     {
                         rs.AddSuccess("Cari silindi");
                     }
                     else
                     {
                          rs.AddError("Silme işlemi başarısız");
                     }
                // }

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }
        public async Task<IActionResult<string>> GetNextCustomerCode()
        {
             var rs = new IActionResult<string> { Result = "" };
             try
             {
                // using (var scope = _serviceScopeFactory.CreateScope())
                // {
                    // var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var uow = _uow;
                    var repo = uow.GetRepository<Customer>();
                    var lastCustomer = await repo.GetAll(disableTracking: true).OrderByDescending(x => x.Id).FirstOrDefaultAsync();

                    if (lastCustomer == null || string.IsNullOrEmpty(lastCustomer.Code))
                    {
                         rs.Result = "120.0001";
                         return rs;
                    }

                    var lastCode = lastCustomer.Code;
                    
                    if (lastCode.Contains("."))
                    {
                        var parts = lastCode.Split('.');
                        if (parts.Length > 1 && long.TryParse(parts.Last(), out long number))
                        {
                             number++;
                             var newLastPart = number.ToString().PadLeft(parts.Last().Length, '0');
                             parts[parts.Length - 1] = newLastPart;
                             rs.Result = string.Join(".", parts);
                             return rs;
                        }
                    }
                    
                    if (long.TryParse(lastCode, out long wholeNumber))
                    {
                         rs.Result = (wholeNumber + 1).ToString();
                         return rs;
                    }

                    rs.Result = "120.0001"; 
                // }
                 return rs;
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "GetNextCustomerCode error");
                 rs.Result = "120.0001"; // Default on error
                 return rs;
             }
        }
        public async Task<IActionResult<List<CustomerSalesPersonDto>>> GetCustomerSalesPersons(int customerId)
        {
            var rs = new IActionResult<List<CustomerSalesPersonDto>> { Result = new List<CustomerSalesPersonDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var mappings = await ctx.CustomerPlasiyers
                    .AsNoTracking()
                    .Include(cp => cp.SalesPerson)
                    .Include(cp => cp.Region)
                    .Where(cp => cp.CustomerId == customerId)
                    .ToListAsync();

                rs.Result = mappings
                    .Select(m => new CustomerSalesPersonDto
                    {
                        Id = m.Id,
                        CustomerId = m.CustomerId,
                        SalesPersonId = m.SalesPersonId,
                        RegionId = m.RegionId,
                        SalesPersonName = $"{m.SalesPerson.FirstName} {m.SalesPerson.LastName}".Trim(),
                        RegionName = m.Region.Name,
                        IsDefault = m.IsDefault
                    })
                    .ToList();

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerSalesPersons error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> AddSalesPersonToCustomer(int customerId, int salesPersonId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var customer = await ctx.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
                if (customer == null)
                {
                    rs.AddError("Cari bulunamadı.");
                    return rs;
                }

                if (!customer.RegionId.HasValue)
                {
                    rs.AddError("Cari için bölge tanımlı değil. Önce cariye bir bölge atayın.");
                    return rs;
                }

                var regionId = customer.RegionId.Value;

                var exists = await ctx.CustomerPlasiyers
                    .AnyAsync(cp => cp.CustomerId == customerId && cp.SalesPersonId == salesPersonId && cp.RegionId == regionId);

                if (exists)
                {
                    rs.AddError("Bu plasiyer zaten bu cari ve bölge ile ilişkilendirilmiş.");
                    return rs;
                }

                var isFirst = !await ctx.CustomerPlasiyers.AnyAsync(cp => cp.CustomerId == customerId);

                var entity = new CustomerPlasiyer
                {
                    CustomerId = customerId,
                    SalesPersonId = salesPersonId,
                    RegionId = regionId,
                    IsDefault = isFirst,
                    CreatedDate = DateTime.UtcNow
                };

                await ctx.CustomerPlasiyers.AddAsync(entity);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Plasiyer cari ile ilişkilendirildi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddSalesPersonToCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> RemoveSalesPersonFromCustomer(int mappingId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var entity = await ctx.CustomerPlasiyers.FirstOrDefaultAsync(cp => cp.Id == mappingId);
                if (entity == null)
                {
                    rs.AddError("Kayıt bulunamadı.");
                    return rs;
                }

                ctx.CustomerPlasiyers.Remove(entity);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Plasiyer bağlantısı kaldırıldı.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveSalesPersonFromCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> SetDefaultSalesPerson(int customerId, int mappingId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var mappings = await ctx.CustomerPlasiyers
                    .Where(cp => cp.CustomerId == customerId)
                    .ToListAsync();

                bool found = false;
                foreach (var m in mappings)
                {
                    if (m.Id == mappingId) found = true;
                    m.IsDefault = m.Id == mappingId;
                }

                if (!found) 
                {
                    // This is the smoking gun check
                    rs.AddError($"Hedef plasiyer kaydı (ID: {mappingId}) bu cari için bulunamadı.");
                    return rs;
                }

                await ctx.SaveChangesAsync();
                rs.AddSuccess("Varsayılan plasiyer ayarlandı.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetDefaultSalesPerson error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<IdentityUserListDto>>> GetCustomerUsers(int customerId)
        {
            var rs = new IActionResult<List<IdentityUserListDto>> { Result = new List<IdentityUserListDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var users = await ctx.AspNetUsers
                    .Where(u => u.CustomerId == customerId)
                    .ToListAsync();

                rs.Result = _mapper.Map<List<IdentityUserListDto>>(users);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerUsers error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<IdentityUserListDto>>> GetAllUsers(int? corporationId = null)
        {
            var rs = new IActionResult<List<IdentityUserListDto>> { Result = new List<IdentityUserListDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var query = ctx.AspNetUsers
                    .Include(u => u.Roles)
                    .AsQueryable();

                // If not global admin, always filter by current corp if no specific corp requested
                if (!corporationId.HasValue && !_tenantProvider.IsGlobalAdmin)
                {
                    corporationId = _tenantProvider.GetCurrentCorporationId();
                }

                if (corporationId.HasValue && corporationId.Value > 0)
                {
                    // Filter users who belong to the specified corporation via UserBranch
                    query = from u in query
                            join ub in ctx.UserBranches on u.Id equals ub.UserId
                            join b in ctx.Branches on ub.BranchId equals b.Id
                            where b.CorporationId == corporationId.Value
                            select u;
                    
                    query = query.Distinct();
                }

                // Filter by relevant roles for B2B Customer Management
                // We typically only want to see B2B users, Plasiyers or Accountants here
                query = query.Where(u => u.Roles.Any(r => 
                    r.Name == "CustomerB2B" || 
                    r.Name.ToUpper() == "B2BADMIN" || 
                    r.Name.ToUpper() == "ACCOUNTANT" || 
                    r.Name == "Accountant" || 
                    r.Name == "Plasiyer"));

                var users = await query
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();

                rs.Result = _mapper.Map<List<IdentityUserListDto>>(users);
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllUsers error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> LinkUserToCustomer(int userId, int customerId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var user = await ctx.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    rs.AddError("Kullanıcı bulunamadı.");
                    return rs;
                }

                user.CustomerId = customerId;
                ctx.AspNetUsers.Update(user);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Kullanıcı cariye bağlandı.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LinkUserToCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UnlinkUserFromCustomer(int userId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                var user = await ctx.AspNetUsers.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    rs.AddError("Kullanıcı bulunamadı.");
                    return rs;
                }

                user.CustomerId = null;
                ctx.AspNetUsers.Update(user);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Kullanıcı bağlantısı kaldırıldı.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnlinkUserFromCustomer error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        // ========== Customer Adres Yönetimi (UserAddress via ApplicationUser) ==========

        public async Task<IActionResult<List<UserAddressListDto>>> GetCustomerAddresses(int customerId)
        {
            var rs = new IActionResult<List<UserAddressListDto>> { Result = new List<UserAddressListDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _uow.DbContext;

                // Customer'a ait tüm ApplicationUser'ları bul
                var customerUserIds = await ctx.AspNetUsers
                    .Where(u => u.CustomerId == customerId)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!customerUserIds.Any())
                {
                    return rs; // Customer'a bağlı kullanıcı yoksa boş liste döndür
                }

                // Bu ApplicationUser'lara ait tüm UserAddress'leri getir
                var addresses = await ctx.UserAddresses
                    .Where(ua => ua.ApplicationUserId.HasValue && customerUserIds.Contains(ua.ApplicationUserId.Value))
                    .Include(ua => ua.City)
                    .Include(ua => ua.Town)
                    .Include(ua => ua.InvoiceCity)
                    .Include(ua => ua.InvoiceTown)
                    .Where(ua => ua.Status == (int)EntityStatus.Active)
                    .OrderByDescending(ua => ua.IsDefault)
                    .ThenBy(ua => ua.CreatedDate)
                    .ToListAsync();

                rs.Result = addresses.Select(a => new UserAddressListDto
                {
                    Id = a.Id,
                    UserId = a.UserId,
                    ApplicationUserId = a.ApplicationUserId,
                    AddressName = a.AddressName ?? "",
                    FullName = a.FullName ?? "",
                    Email = a.Email ?? "",
                    PhoneNumber = a.PhoneNumber ?? "",
                    Address = a.Address ?? "",
                    CityId = a.CityId,
                    CityName = a.City?.Name,
                    TownId = a.TownId,
                    TownName = a.Town?.Name,
                    IdentityNumber = a.IdentityNumber,
                    IsDefault = a.IsDefault,
                    IsSameAsDeliveryAddress = a.IsSameAsDeliveryAddress,
                    InvoiceCityId = a.InvoiceCityId,
                    InvoiceCityName = a.InvoiceCity?.Name,
                    InvoiceTownId = a.InvoiceTownId,
                    InvoiceTownName = a.InvoiceTown?.Name,
                    InvoiceAddress = a.InvoiceAddress,
                    Status = a.Status
                }).ToList();

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerAddresses error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<UserAddressUpsertDto>> GetCustomerAddressById(int addressId)
        {
            var rs = new IActionResult<UserAddressUpsertDto> { Result = new UserAddressUpsertDto() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var address = await ctx.UserAddresses
                    .Include(a => a.City)
                    .Include(a => a.Town)
                    .Include(a => a.InvoiceCity)
                    .Include(a => a.InvoiceTown)
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.Status == (int)EntityStatus.Active);

                if (address == null)
                {
                    rs.AddError("Adres bulunamadı.");
                    return rs;
                }

                rs.Result = new UserAddressUpsertDto
                {
                    Id = address.Id,
                    UserId = address.UserId,
                    ApplicationUserId = address.ApplicationUserId,
                    AddressName = address.AddressName ?? "",
                    FullName = address.FullName ?? "",
                    Email = address.Email ?? "",
                    PhoneNumber = address.PhoneNumber ?? "",
                    Address = address.Address ?? "",
                    CityId = address.CityId,
                    TownId = address.TownId,
                    IdentityNumber = address.IdentityNumber,
                    IsDefault = address.IsDefault,
                    IsSameAsDeliveryAddress = address.IsSameAsDeliveryAddress,
                    InvoiceCityId = address.InvoiceCityId,
                    InvoiceTownId = address.InvoiceTownId,
                    InvoiceAddress = address.InvoiceAddress
                };

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetCustomerAddressById error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<int>> AddCustomerAddress(int customerId, UserAddressUpsertDto addressDto)
        {
            var rs = new IActionResult<int> { Result = 0 };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Customer'ın ilk ApplicationUser'ını bul (veya tüm ApplicationUser'ları)
                var customerUser = await ctx.AspNetUsers
                    .FirstOrDefaultAsync(u => u.CustomerId == customerId);

                if (customerUser == null)
                {
                    rs.AddError("Cariye bağlı kullanıcı bulunamadı. Önce kullanıcı bağlamalısınız.");
                    return rs;
                }

                // Eğer varsayılan adres seçiliyse, diğer adreslerin IsDefault'ını false yap
                if (addressDto.IsDefault)
                {
                    var existingAddresses = await ctx.UserAddresses
                        .Where(ua => ua.ApplicationUserId == customerUser.Id && ua.Status == (int)EntityStatus.Active)
                        .ToListAsync();

                    foreach (var addr in existingAddresses)
                    {
                        addr.IsDefault = false;
                    }
                }

                var address = new UserAddress
                {
                    ApplicationUserId = customerUser.Id,
                    AddressName = addressDto.AddressName,
                    FullName = addressDto.FullName,
                    Email = addressDto.Email,
                    PhoneNumber = addressDto.PhoneNumber,
                    Address = addressDto.Address,
                    CityId = addressDto.CityId,
                    TownId = addressDto.TownId,
                    IdentityNumber = addressDto.IdentityNumber,
                    IsDefault = addressDto.IsDefault,
                    IsSameAsDeliveryAddress = addressDto.IsSameAsDeliveryAddress,
                    InvoiceCityId = addressDto.InvoiceCityId,
                    InvoiceTownId = addressDto.InvoiceTownId,
                    InvoiceAddress = addressDto.InvoiceAddress,
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = 1 // TODO: Get from current user context
                };

                await ctx.UserAddresses.AddAsync(address);
                await ctx.SaveChangesAsync();

                rs.Result = address.Id;
                rs.AddSuccess("Adres başarıyla eklendi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddCustomerAddress error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpdateCustomerAddress(int addressId, UserAddressUpsertDto addressDto)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var address = await ctx.UserAddresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.Status == (int)EntityStatus.Active);

                if (address == null)
                {
                    rs.AddError("Adres bulunamadı.");
                    return rs;
                }

                // Eğer varsayılan adres seçiliyse, diğer adreslerin IsDefault'ını false yap
                if (addressDto.IsDefault && !address.IsDefault)
                {
                    var customerUser = address.ApplicationUserId;
                    if (customerUser.HasValue)
                    {
                        var existingAddresses = await ctx.UserAddresses
                            .Where(ua => ua.ApplicationUserId == customerUser.Value && 
                                        ua.Id != addressId && 
                                        ua.Status == (int)EntityStatus.Active)
                            .ToListAsync();

                        foreach (var addr in existingAddresses)
                        {
                            addr.IsDefault = false;
                        }
                    }
                }

                address.AddressName = addressDto.AddressName;
                address.FullName = addressDto.FullName;
                address.Email = addressDto.Email;
                address.PhoneNumber = addressDto.PhoneNumber;
                address.Address = addressDto.Address;
                address.CityId = addressDto.CityId;
                address.TownId = addressDto.TownId;
                address.IdentityNumber = addressDto.IdentityNumber;
                address.IsDefault = addressDto.IsDefault;
                address.IsSameAsDeliveryAddress = addressDto.IsSameAsDeliveryAddress;
                address.InvoiceCityId = addressDto.InvoiceCityId;
                address.InvoiceTownId = addressDto.InvoiceTownId;
                address.InvoiceAddress = addressDto.InvoiceAddress;
                address.ModifiedDate = DateTime.Now;
                address.ModifiedId = 1; // TODO: Get from current user context

                ctx.UserAddresses.Update(address);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Adres başarıyla güncellendi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateCustomerAddress error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteCustomerAddress(int addressId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var address = await ctx.UserAddresses
                    .FirstOrDefaultAsync(a => a.Id == addressId && a.Status == (int)EntityStatus.Active);

                if (address == null)
                {
                    rs.AddError("Adres bulunamadı.");
                    return rs;
                }

                // Soft delete
                address.Status = (int)EntityStatus.Deleted;
                address.DeletedDate = DateTime.Now;
                address.DeletedId = 1; // TODO: Get from current user context

                ctx.UserAddresses.Update(address);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Adres başarıyla silindi.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCustomerAddress error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> SetDefaultCustomerAddress(int customerId, int addressId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Customer'ın tüm ApplicationUser'larını bul
                var customerUserIds = await ctx.AspNetUsers
                    .Where(u => u.CustomerId == customerId)
                    .Select(u => u.Id)
                    .ToListAsync();

                if (!customerUserIds.Any())
                {
                    rs.AddError("Cariye bağlı kullanıcı bulunamadı.");
                    return rs;
                }

                // Tüm adreslerin IsDefault'ını false yap
                var allAddresses = await ctx.UserAddresses
                    .Where(ua => ua.ApplicationUserId.HasValue && 
                                customerUserIds.Contains(ua.ApplicationUserId.Value) &&
                                ua.Status == (int)EntityStatus.Active)
                    .ToListAsync();

                foreach (var addr in allAddresses)
                {
                    addr.IsDefault = false;
                }

                // Seçilen adresi varsayılan yap
                var targetAddress = allAddresses.FirstOrDefault(a => a.Id == addressId);
                if (targetAddress == null)
                {
                    rs.AddError("Adres bulunamadı.");
                    return rs;
                }

                targetAddress.IsDefault = true;
                targetAddress.ModifiedDate = DateTime.Now;
                targetAddress.ModifiedId = 1; // TODO: Get from current user context

                ctx.UserAddresses.UpdateRange(allAddresses);
                await ctx.SaveChangesAsync();

                rs.AddSuccess("Varsayılan adres başarıyla ayarlandı.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetDefaultCustomerAddress error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        #region Private Helper Methods for Role-Based Filtering



        #endregion
    }
}
