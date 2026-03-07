using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Admin.Domain.Dtos.MonthDto;
using ecommerce.Admin.Domain.Dtos.CustomerWorkPlanDto;
// using ecommerce.Admin.Domain.Dtos.Plasiyer; // Plasiyer rota devre dışı
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Core.Interfaces;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Concreate
{
    public class SalesPersonService : ISalesPersonService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<SalesPerson> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<SalesPersonListDto> _radzenPagerService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CurrentUser _currentUser;
        private readonly ecommerce.Admin.Domain.Services.IRoleBasedFilterService _roleFilter;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "salespersons";

        public SalesPersonService(
            IUnitOfWork<ApplicationDbContext> context, 
            IMapper mapper, 
            ILogger logger, 
            IRadzenPagerService<SalesPersonListDto> radzenPagerService, 
            IServiceScopeFactory serviceScopeFactory,
            ITenantProvider tenantProvider,
            IHttpContextAccessor httpContextAccessor,
            CurrentUser currentUser,
            ecommerce.Admin.Domain.Services.IRoleBasedFilterService roleFilter,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<SalesPerson>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _serviceScopeFactory = serviceScopeFactory;
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
            _currentUser = currentUser;
            _roleFilter = roleFilter;
            _permissionService = permissionService;
        }

        private async Task<bool> CanCreate() => await _permissionService.CanCreate(MENU_NAME);
        private async Task<bool> CanEdit() => await _permissionService.CanEdit(MENU_NAME);
        private async Task<bool> CanDelete() => await _permissionService.CanDelete(MENU_NAME);
        private async Task<bool> CanView() => await _permissionService.CanView(MENU_NAME);

        public async Task<IActionResult<Paging<IQueryable<SalesPersonListDto>>>> GetSalesPersons(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<SalesPersonListDto>>> response = new() { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    include: q => q.Include(s => s.Branch).Include(s => s.City).Include(s => s.Town),
                    ignoreQueryFilters: true
                );

                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<SalesPersonListDto>>(items);
                if (mapped?.Count > 0)
                {
                    response.Result.Data = mapped.AsQueryable();
                    response.Result.Data = response.Result.Data?.OrderByDescending(x => x.Id);
                }

                response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSalesPersons Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<SalesPersonListDto>>> GetSalesPersons()
        {
            var response = new IActionResult<List<SalesPersonListDto>> { Result = new List<SalesPersonListDto>() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                // using var scope = _serviceScopeFactory.CreateScope();
                
                // Transfer user context to the new scope
                // var scopedCurrentUser = scope.ServiceProvider.GetRequiredService<CurrentUser>();
                // scopedCurrentUser.SetUser(_currentUser.Principal);

                // var ctx = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var ctx = _context;
                var repo = ctx.GetRepository<SalesPerson>();
                // var roleFilter = scope.ServiceProvider.GetRequiredService<ecommerce.Admin.Domain.Services.IRoleBasedFilterService>();
                var roleFilter = _roleFilter;
                
                var query = repo.GetAll(
                    predicate: x => x.Status == (int)EntityStatus.Active,
                    disableTracking: true,
                    include: q => q.Include(s => s.Branch).Include(s => s.City).Include(s => s.Town),
                    ignoreQueryFilters: true
                );

                query = roleFilter.ApplyFilter(query, ctx.DbContext);
                
                var items = await query.ToListAsync();
                var mapped = _mapper.Map<List<SalesPersonListDto>>(items);
                if (mapped?.Count > 0) response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSalesPersons Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<SalesPersonUpsertDto>> GetSalesPersonById(int id)
        {
            var response = new IActionResult<SalesPersonUpsertDto> { Result = new() };
            try
            {
                if (!await CanView())
                {
                    response.AddError("Görüntüleme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: x => x.Id == id,
                    include: x => x.Include(s => s.Branch).Include(s => s.City).Include(s => s.Town)
                                   .Include(s => s.SalesPersonBranches).ThenInclude(sb => sb.Branch).ThenInclude(b => b.Corporation),
                    ignoreQueryFilters: true
                );

                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    // Check existance for security msg
                    var exists = await _repository.GetAll(predicate: x => x.Id == id, ignoreQueryFilters: true).AnyAsync();
                    if (exists)
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext))
                {
                        response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                        return response;
                }
                var mapped = _mapper.Map<SalesPersonUpsertDto>(entity);
                if (mapped != null)
                {
                    if (entity.SalesPersonBranches != null)
                    {
                        mapped.Branches = entity.SalesPersonBranches.Where(sb => sb.Status != (int)EntityStatus.Deleted).Select(sb => new SalesPersonBranchUpsertDto
                        {
                            Id = sb.Id,
                            SalesPersonId = sb.SalesPersonId,
                            BranchId = sb.BranchId,
                            BranchName = sb.Branch?.Name,
                            CorporationId = sb.Branch?.CorporationId ?? 0,
                            CorporationName = sb.Branch?.Corporation?.Name,
                            IsDefault = sb.IsDefault
                        }).ToList();
                    }
                    response.Result = mapped;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSalesPersonById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertSalesPerson(AuditWrapDto<SalesPersonUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var dto = model.Dto;
                SalesPerson salesPersonEntity;

                if (!dto.Id.HasValue)
                {
                   if (!await CanCreate())
                   {
                       response.AddError("Ekleme yetkiniz bulunmamaktadır.");
                       return response;
                   }

                    salesPersonEntity = _mapper.Map<SalesPerson>(dto);
                    salesPersonEntity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    salesPersonEntity.CreatedId = model.UserId;
                    salesPersonEntity.CreatedDate = DateTime.Now;
                    
                    var currentBranchId = _tenantProvider.GetCurrentBranchId();
                    salesPersonEntity.BranchId = currentBranchId;

                    await _repository.InsertAsync(salesPersonEntity);
                }
                else
                {
                    if (!await CanEdit())
                    {
                        response.AddError("Düzenleme yetkiniz bulunmamaktadır.");
                        return response;
                    }

                    var query = _repository.GetAll(
                        predicate: x => x.Id == dto.Id && x.Status != (int)EntityStatus.Deleted,
                        disableTracking: false,
                        ignoreQueryFilters: true);

                    query = _roleFilter.ApplyFilter(query, _context.DbContext);
                    
                    salesPersonEntity = await query.FirstOrDefaultAsync();

                    if (salesPersonEntity == null)
                    {
                        response.AddError("SalesPerson bulunamadı veya yetkiniz yok.");
                        return response;
                    }

                    if (!await _roleFilter.CanAccessBranchAsync(salesPersonEntity.BranchId ?? 0, _context.DbContext))
                    {
                         response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                         return response;
                    }

                    _mapper.Map(dto, salesPersonEntity);
                    
                    salesPersonEntity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    salesPersonEntity.ModifiedId = model.UserId;
                    salesPersonEntity.ModifiedDate = DateTime.Now;

                    if (_tenantProvider.IsGlobalAdmin)
                    {
                        salesPersonEntity.BranchId =0;
                    }
                }

                if (dto.Branches != null)
                {
                    var branchRepo = _context.GetRepository<SalesPersonBranch>();
                    
                    // We need to fetch existing branches ONLY if we have an ID (update scenario)
                    // If it's a new SalesPerson, there are no existing branches in DB.
                    var existingBranches = new List<SalesPersonBranch>();
                    
                    if (salesPersonEntity.Id > 0)
                    {
                        existingBranches = await branchRepo.GetAll(disableTracking: false) // Tracked for updates
                            .Where(sb => sb.SalesPersonId == salesPersonEntity.Id && sb.Status != (int)EntityStatus.Deleted)
                            .ToListAsync();
                    }

                    // Delete removed branches
                    var branchIdsToKeep = dto.Branches.Where(b => b.Id.HasValue).Select(b => b.Id.Value).ToList();
                    var branchesToDelete = existingBranches.Where(eb => !branchIdsToKeep.Contains(eb.Id)).ToList();
                    
                    foreach (var branch in branchesToDelete)
                    {
                        branch.Status = (int)EntityStatus.Deleted;
                        branch.DeletedDate = DateTime.Now;
                        branch.ModifiedId = model.UserId;
                        // branchRepo.Update(branch); // Tracked
                    }

                    // Upsert branches
                    foreach (var branchDto in dto.Branches)
                    {
                        if (branchDto.Id.HasValue && branchDto.Id.Value > 0)
                        {
                            var existing = existingBranches.FirstOrDefault(eb => eb.Id == branchDto.Id.Value);
                            if (existing != null)
                            {
                                existing.BranchId = branchDto.BranchId;
                                existing.IsDefault = branchDto.IsDefault;
                                existing.ModifiedDate = DateTime.Now;
                                existing.ModifiedId = model.UserId;
                                // branchRepo.Update(existing); // Tracked
                            }
                        }
                        else
                        {
                            var newBranch = new SalesPersonBranch
                            {
                                BranchId = branchDto.BranchId,
                                IsDefault = branchDto.IsDefault,
                                Status = (int)EntityStatus.Active,
                                CreatedDate = DateTime.Now,
                                CreatedId = model.UserId
                            };

                            if (salesPersonEntity.Id > 0)
                            {
                                newBranch.SalesPersonId = salesPersonEntity.Id;
                            }
                            else
                            {
                                newBranch.SalesPerson = salesPersonEntity;
                            }
                            
                            await branchRepo.InsertAsync(newBranch);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertSalesPerson Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteSalesPerson(AuditWrapDto<SalesPersonDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                if (!await CanDelete())
                {
                    response.AddError("Silme yetkiniz bulunmamaktadır.");
                    return response;
                }

                var query = _repository.GetAll(
                    predicate: x => x.Id == model.Dto.Id,
                    ignoreQueryFilters: true);
                
                query = _roleFilter.ApplyFilter(query, _context.DbContext);
                
                var entity = await query.FirstOrDefaultAsync();

                if (entity == null)
                {
                    response.AddError("Kayıt bulunamadı veya yetkiniz yok.");
                    return response;
                }

                if (!await _roleFilter.CanAccessBranchAsync(entity.BranchId ?? 0, _context.DbContext))
                {
                     response.AddError("Bu işlem için yetkiniz bulunmamaktadır (Şube Yetkisi).");
                     return response;
                }

                await _context.DbContext.SalesPersons.Where(f => f.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(x => x.SetProperty(x => x.DeletedId, model.UserId)
                        .SetProperty(x => x.DeletedDate, DateTime.Now)
                        .SetProperty(x => x.Status, EntityStatus.Deleted.GetHashCode()));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteSalesPerson Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CustomerListDto>>> GetCustomersByRegion(int regionId)
        {
            var response = new IActionResult<List<CustomerListDto>> { Result = new List<CustomerListDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _context.DbContext;

                var customers = await ctx.Customers
                    .Include(c => c.Region)
                    .Include(c => c.City)
                    .Include(c => c.Town)
                    .Where(c => c.RegionId.HasValue && c.RegionId.Value == regionId && c.IsActive && c.Status != (int)EntityStatus.Deleted)
                    .ToListAsync();

                var mapped = _mapper.Map<List<CustomerListDto>>(customers);
                response.Result = mapped ?? new List<CustomerListDto>();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCustomersByRegion Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CustomerListDto>>> GetCustomersOfSalesPerson(int salesPersonId)
        {
            var response = new IActionResult<List<CustomerListDto>> { Result = new List<CustomerListDto>() };
            try
            {
                // using var scope = _serviceScopeFactory.CreateScope();
                // var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var ctx = _context.DbContext;

                // CRITICAL: Filter by Current Branch ID
                var currentBranchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0;

                var customers = await ctx.CustomerPlasiyers
                    .Include(cp => cp.Customer).ThenInclude(c => c.Region)
                    .Include(cp => cp.Customer).ThenInclude(c => c.City)
                    .Include(cp => cp.Customer).ThenInclude(c => c.Town)
                    .Where(cp => cp.SalesPersonId == salesPersonId && 
                                 (currentBranchId == 0 || cp.Customer.CustomerBranches.Any(cb => cb.BranchId == currentBranchId)))
                    .Select(cp => cp.Customer)
                    .ToListAsync();

                var mapped = _mapper.Map<List<CustomerListDto>>(customers);
                response.Result = mapped ?? new List<CustomerListDto>();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCustomersOfSalesPerson Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        /// <summary>
        /// Harita ekranı için plasiyere bağlı carileri adres bilgileriyle getirir.
        /// Mobil taraf adres bilgisinden geocoding yapacak.
        /// </summary>
        public async Task<IActionResult<List<CustomerWithCoordsDto>>> GetCustomersWithCoordsOfSalesPerson(int salesPersonId)
        {
            var response = new IActionResult<List<CustomerWithCoordsDto>> { Result = new List<CustomerWithCoordsDto>() };
            try
            {
                var ctx = _context.DbContext;

                // Şube filtreleme
                var currentBranchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0;

                var customers = await ctx.CustomerPlasiyers
                    .AsNoTracking()
                    .Include(cp => cp.Customer).ThenInclude(c => c.City)
                    .Include(cp => cp.Customer).ThenInclude(c => c.Town)
                    .Where(cp => cp.SalesPersonId == salesPersonId &&
                                 (currentBranchId == 0 || cp.Customer.CustomerBranches.Any(cb => cb.BranchId == currentBranchId)))
                    .Select(cp => new CustomerWithCoordsDto
                    {
                        Id = cp.Customer.Id,
                        Code = cp.Customer.Code,
                        Name = cp.Customer.Name,
                        CityName = cp.Customer.City != null ? cp.Customer.City.Name : null,
                        TownName = cp.Customer.Town != null ? cp.Customer.Town.Name : null,
                        Address = cp.Customer.Address,
                        Phone = cp.Customer.Phone
                    })
                    .ToListAsync();

                response.Result = customers;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCustomersWithCoordsOfSalesPerson Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> AssignCustomersToSalesPerson(int salesPersonId, int regionId)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var customerIds = await ctx.Customers
                    .Where(c => c.RegionId.HasValue && c.RegionId.Value == regionId && c.IsActive && c.Status != (int)EntityStatus.Deleted)
                    .Select(c => c.Id)
                    .ToListAsync();

                if (customerIds.Count == 0)
                {
                    response.AddError("Seçilen bölgede aktif cari bulunamadı.");
                    return response;
                }

                var existing = await ctx.CustomerPlasiyers
                    .Where(cp => cp.SalesPersonId == salesPersonId && customerIds.Contains(cp.CustomerId))
                    .Select(cp => cp.CustomerId)
                    .ToListAsync();

                var newIds = customerIds.Except(existing).ToList();
                if (newIds.Count > 0)
                {
                    var toInsert = newIds.Select(cid => new CustomerPlasiyer
                    {
                        CustomerId = cid,
                        SalesPersonId = salesPersonId,
                        RegionId = regionId,
                        CreatedDate = DateTime.UtcNow
                    });

                    await ctx.CustomerPlasiyers.AddRangeAsync(toInsert);
                    await ctx.SaveChangesAsync();
                }

                response.AddSuccess("Cari(ler) plasiyere bağlandı.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("AssignCustomersToSalesPerson Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<MonthListDto>>> GetMonths()
        {
            var response = new IActionResult<List<MonthListDto>> { Result = new List<MonthListDto>() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var months = await ctx.Months
                    .OrderBy(m => m.Order)
                    .ToListAsync();

                if (!months.Any())
                {
                    // Ayları otomatik oluştur
                    var turkishMonths = new[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
                    var monthEntities = turkishMonths.Select((name, index) => new Month
                    {
                        Name = name,
                        MonthNumber = index + 1,
                        Order = index + 1
                    }).ToList();

                    await ctx.Months.AddRangeAsync(monthEntities);
                    await ctx.SaveChangesAsync();
                    months = monthEntities;
                }

                var mapped = _mapper.Map<List<MonthListDto>>(months);
                response.Result = mapped ?? new List<MonthListDto>();
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetMonths Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CustomerWorkPlanListDto>>> GetWorkPlansBySalesPerson(int salesPersonId)
        {
            var response = new IActionResult<List<CustomerWorkPlanListDto>> { Result = new List<CustomerWorkPlanListDto>() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var workPlans = await ctx.CustomerWorkPlans
                    .Include(wp => wp.SalesPerson)
                    .Include(wp => wp.Customer)
                    .Include(wp => wp.Month)
                    .Where(wp => wp.SalesPersonId == salesPersonId)
                    .OrderBy(wp => wp.Month.Order)
                    .ThenBy(wp => wp.DayOfWeek)
                    .ToListAsync();

                var culture = new System.Globalization.CultureInfo("tr-TR");
                var dayNames = new[] { "Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi" };

                var mapped = workPlans.Select(wp => new CustomerWorkPlanListDto
                {
                    Id = wp.Id,
                    SalesPersonId = wp.SalesPersonId,
                    SalesPersonName = wp.SalesPerson != null ? $"{wp.SalesPerson.FirstName} {wp.SalesPerson.LastName}" : "",
                    CustomerId = wp.CustomerId,
                    CustomerName = wp.Customer?.Name ?? "",
                    CustomerCode = wp.Customer?.Code ?? "",
                    DayOfWeek = wp.DayOfWeek,
                    DayName = wp.DayOfWeek >= 0 && wp.DayOfWeek < dayNames.Length ? dayNames[wp.DayOfWeek] : "",
                    MonthId = wp.MonthId,
                    MonthName = wp.Month?.Name ?? "",
                    CreatedDate = wp.CreatedDate
                }).ToList();

                response.Result = mapped;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetWorkPlansBySalesPerson Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertWorkPlan(AuditWrapDto<CustomerWorkPlanUpsertDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var dto = model.Dto;

                // Aynı plasiyer, cari, gün ve ay kombinasyonu kontrolü
                var existing = await ctx.CustomerWorkPlans
                    .FirstOrDefaultAsync(wp => wp.SalesPersonId == dto.SalesPersonId
                        && wp.CustomerId == dto.CustomerId
                        && wp.DayOfWeek == dto.DayOfWeek
                        && wp.MonthId == dto.MonthId
                        && (!dto.Id.HasValue || wp.Id != dto.Id.Value));

                if (existing != null)
                {
                    response.AddError("Bu cari için seçilen gün ve ay kombinasyonu zaten mevcut.");
                    return response;
                }

                if (!dto.Id.HasValue)
                {
                    var entity = new CustomerWorkPlan
                    {
                        SalesPersonId = dto.SalesPersonId,
                        CustomerId = dto.CustomerId,
                        DayOfWeek = dto.DayOfWeek,
                        MonthId = dto.MonthId,
                        CreatedDate = DateTime.UtcNow
                    };
                    await ctx.CustomerWorkPlans.AddAsync(entity);
                }
                else
                {
                    var current = await ctx.CustomerWorkPlans
                        .FirstOrDefaultAsync(wp => wp.Id == dto.Id.Value);

                    if (current == null)
                    {
                        response.AddError("Çalışma planı bulunamadı.");
                        return response;
                    }

                    current.SalesPersonId = dto.SalesPersonId;
                    current.CustomerId = dto.CustomerId;
                    current.DayOfWeek = dto.DayOfWeek;
                    current.MonthId = dto.MonthId;
                }

                await ctx.SaveChangesAsync();
                response.AddSuccess("Çalışma planı kaydedildi.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertWorkPlan Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteWorkPlan(AuditWrapDto<CustomerWorkPlanDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var workPlan = await ctx.CustomerWorkPlans
                    .FirstOrDefaultAsync(wp => wp.Id == model.Dto.Id);

                if (workPlan == null)
                {
                    response.AddError("Çalışma planı bulunamadı.");
                    return response;
                }

                ctx.CustomerWorkPlans.Remove(workPlan);
                await ctx.SaveChangesAsync();

                response.AddSuccess("Çalışma planı silindi.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteWorkPlan Exception " + ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        // Plasiyer rota - şimdilik devre dışı
        // public Task<IActionResult<List<PlasiyerRotaCustomerDto>>> GetPlasiyerRotaList(int salesPersonId)
        // {
        //     var response = new IActionResult<List<PlasiyerRotaCustomerDto>> { Result = new List<PlasiyerRotaCustomerDto>() };
        //     return Task.FromResult(response);
        // }
        // public Task<IActionResult<List<PlasiyerCustomerVisitDto>>> GetCustomerVisitDetails(int customerId, int salesPersonId)
        // {
        //     var response = new IActionResult<List<PlasiyerCustomerVisitDto>> { Result = new List<PlasiyerCustomerVisitDto>() };
        //     return Task.FromResult(response);
        // }
        // public Task<IActionResult<Empty>> SaveCustomerVisit(int customerId, int salesPersonId, string visitNote)
        // {
        //     var response = new IActionResult<Empty> { Result = new Empty() };
        //     response.AddError("Plasiyer rota özelliği şu an devre dışıdır.");
        //     return Task.FromResult(response);
        // }
    }
}
 