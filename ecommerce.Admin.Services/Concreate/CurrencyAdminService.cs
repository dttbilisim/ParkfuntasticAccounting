using AutoMapper;
using CurrencyAuto.Abstract;
using ecommerce.Admin.Domain.Dtos.CurrencyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ecommerce.Admin.Services.Concreate;

public class CurrencyAdminService : ICurrencyAdminService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<Currency> _repository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<CurrencyListDto> _radzenPagerService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ICurrencyService _currencyService;
    private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
    private const string MENU_NAME = "currencies";

    public CurrencyAdminService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<CurrencyListDto> radzenPagerService,
        IServiceScopeFactory serviceScopeFactory,
        ICurrencyService currencyService,
        ecommerce.Admin.Domain.Services.IPermissionService permissionService)
    {
        _context = context;
        _repository = context.GetRepository<Currency>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _serviceScopeFactory = serviceScopeFactory;
        _currencyService = currencyService;
        _permissionService = permissionService;
    }

    public async Task<IActionResult<Paging<IQueryable<CurrencyListDto>>>> GetCurrencies(PageSetting pager)
    {
        IActionResult<Paging<IQueryable<CurrencyListDto>>> response = new() { Result = new() };
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var scopedRepo = scopedUow.GetRepository<Currency>();

            var entities = await scopedRepo.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active);
            var mappedList = _mapper.Map<List<CurrencyListDto>>(entities);
            if (mappedList != null && mappedList.Count > 0)
            {
                response.Result.Data = mappedList.AsQueryable().OrderByDescending(x => x.Id);
            }

            var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            response.Result = result;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCurrencies Exception {Ex}", ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<List<CurrencyListDto>>> GetCurrencies()
    {
        var rs = new IActionResult<List<CurrencyListDto>> { Result = new() };
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var scopedRepo = scopedUow.GetRepository<Currency>();

            var entities = await scopedRepo.GetAllAsync(predicate: x => x.Status == (int)EntityStatus.Active);
            var mapped = _mapper.Map<List<CurrencyListDto>>(entities);
            if (mapped != null && mapped.Count > 0)
            {
                rs.Result = mapped
                    .OrderBy(x => x.CurrencyCode)
                    .ToList();
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCurrencies (list) Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<CurrencyUpsertDto>> GetCurrencyById(int id)
    {
        var rs = new IActionResult<CurrencyUpsertDto> { Result = new() };
        try
        {
            var entity = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == id);
            var mapped = _mapper.Map<CurrencyUpsertDto>(entity);
            if (mapped != null)
            {
                rs.Result = mapped;
            }
            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetCurrencyById Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> UpsertCurrency(AuditWrapDto<CurrencyUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            var dto = model.Dto;

            if (!dto.Id.HasValue)
            {
                // Check for duplicate currency code globally
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(c => c.CurrencyCode.ToLower() == dto.CurrencyCode.ToLower() && c.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.CurrencyCode}' kodlu kur zaten mevcut.");
                    return rs;
                }

                var entity = _mapper.Map<Currency>(dto);
                entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                entity.IsStatic = dto.IsStatic;
                entity.CreatedId = model.UserId;
                entity.CreatedDate = DateTime.Now;

                await _repository.InsertAsync(entity);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Check for duplicate currency code globally (excluding current entity)
                var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                    .AnyAsync(c => c.Id != dto.Id && c.CurrencyCode.ToLower() == dto.CurrencyCode.ToLower() && c.Status != (int)EntityStatus.Deleted);
                if (duplicate)
                {
                    rs.AddError($"'{dto.CurrencyCode}' kodlu kur zaten mevcut.");
                    return rs;
                }

                await _context.DbContext.Currencies
                    .Where(x => x.Id == dto.Id)
                    .ExecuteUpdateAsync(x => x
                        .SetProperty(c => c.CurrencyCode, dto.CurrencyCode)
                        .SetProperty(c => c.CurrencyName, dto.CurrencyName)
                        .SetProperty(c => c.ForexBuying, dto.ForexBuying)
                        .SetProperty(c => c.ForexSelling, dto.ForexSelling)
                        .SetProperty(c => c.BanknoteBuying, dto.BanknoteBuying)
                        .SetProperty(c => c.BanknoteSelling, dto.BanknoteSelling)
                        .SetProperty(c => c.IsStatic, dto.IsStatic)
                        .SetProperty(c => c.Status, dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive)
                        .SetProperty(c => c.ModifiedId, model.UserId)
                        .SetProperty(c => c.ModifiedDate, DateTime.Now));

                await _context.SaveChangesAsync();
            }

            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Kur kaydedildi.");
                return rs;
            }
            else
            {
                if (lastResult != null && lastResult.Exception != null)
                {
                    if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                    {
                        rs.AddError($"'{dto.CurrencyCode}' kodlu kur zaten mevcut.");
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
            _logger.LogError("UpsertCurrency Exception {Ex}", ex.ToString());
            if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                rs.AddError($"'{model.Dto.CurrencyCode}' kodlu kur zaten mevcut.");
            }
            else
            {
                rs.AddSystemError(ex.ToString());
            }
            return rs;
        }
    }

    public async Task<IActionResult<Empty>> DeleteCurrency(AuditWrapDto<CurrencyDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            await _context.DbContext.Currencies
                .Where(f => f.Id == model.Dto.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                    .SetProperty(a => a.DeletedDate, DateTime.Now)
                    .SetProperty(a => a.DeletedId, model.UserId));

            await _context.SaveChangesAsync();
            var lastResult = _context.LastSaveChangesResult;
            if (lastResult.IsOk)
            {
                rs.AddSuccess("Kur silindi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("DeleteCurrency Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }

    /// <summary>
    /// Merkez Bankası entegrasyonundan (ICurrencyService) anlık kurları çeker
    /// ve admin tarafındaki Currency kayıtlarını günceller.
    /// IsStatic == true olan kayıtlar asla dokunulmaz.
    /// </summary>
    public async Task<IActionResult<Empty>> RefreshCurrenciesFromCurrencyData(int userId)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var scopedUow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var db = scopedUow.DbContext;
            var integrationService = scope.ServiceProvider.GetRequiredService<ICurrencyService>();

            // 1) Merkez Bankasından güncel kurları çek
            var latestRates = await integrationService.GetTodayRatesAsync();
            if (latestRates == null || latestRates.Count == 0)
            {
                rs.AddError("Merkez Bankası'ndan kur bilgisi alınamadı.");
                return rs;
            }

            // Lookup: CurrencyCode -> gelen kur
            var rateLookup = latestRates
                .GroupBy(x => x.CurrencyCode)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            // 2) Admin ekranında gösterilen Currency tablomuza gidiyoruz
            var currencies = await db.Currencies
                .AsTracking()
                .Where(c => c.Status == (int)EntityStatus.Active)
                .ToListAsync();

            var updatedCount = 0;

            // Eğer tabloda hiç kur yoksa, mevcut kayıtları güncellemek yerine TCMB'den gelen kurlarla
            // ilk kez dolduruyoruz.
            if (currencies.Count == 0)
            {
                foreach (var rate in latestRates)
                {
                    if (string.IsNullOrWhiteSpace(rate.CurrencyCode))
                        continue;

                    var hasValidRate =
                        rate.ForexBuying != 0 ||
                        rate.ForexSelling != 0 ||
                        rate.BanknoteBuying != 0 ||
                        rate.BanknoteSelling != 0;

                    if (!hasValidRate)
                        continue;

                    var entity = new Currency
                    {
                        CurrencyCode = rate.CurrencyCode,
                        CurrencyName = rate.CurrencyName,
                        ForexBuying = rate.ForexBuying,
                        ForexSelling = rate.ForexSelling,
                        BanknoteBuying = rate.BanknoteBuying,
                        BanknoteSelling = rate.BanknoteSelling,
                        IsStatic = false,
                        Status = (int)EntityStatus.Active,
                        CreatedDate = DateTime.Now,
                        CreatedId = userId
                    };

                    await db.Currencies.AddAsync(entity);
                    updatedCount++;
                }
            }
            else
            foreach (var cur in currencies)
            {
                // Sabit işaretli olanları atla
                if (cur.IsStatic)
                    continue;

                if (string.IsNullOrWhiteSpace(cur.CurrencyCode))
                    continue;

                if (rateLookup.TryGetValue(cur.CurrencyCode, out var rate))
                {
                    // TCMB'den gelen değerleri her durumda güncelliyoruz.
                    // Böylece elle değiştirilen bir kur da butona basıldığında tekrar TCMB değerine döner.
                    cur.ForexBuying = rate.ForexBuying;
                    cur.ForexSelling = rate.ForexSelling;
                    cur.BanknoteBuying = rate.BanknoteBuying;
                    cur.BanknoteSelling = rate.BanknoteSelling;
                    cur.ModifiedDate = DateTime.Now;
                    cur.ModifiedId = userId;
                    updatedCount++;
                }
            }

            // Hiç kayıt güncellenemediyse kullanıcıyı bilgilendir
            if (updatedCount == 0)
            {
                rs.AddError("Hiçbir kur güncellenmedi. Lütfen CurrencyCode değerlerinin TCMB kodlarıyla eşleştiğini ve IsStatic alanının güncellenmesini istediğiniz kurlar için pasif olduğunu kontrol edin.");
                return rs;
            }

            await scopedUow.SaveChangesAsync();
            var lastResult = scopedUow.LastSaveChangesResult;

            if (lastResult.IsOk)
            {
                rs.AddSuccess("Güncel kurlar başarıyla çekildi. Sabit işaretli kurlar güncellenmedi.");
                return rs;
            }

            if (lastResult.Exception != null)
                rs.AddError(lastResult.Exception.ToString());

            return rs;
        }
        catch (Exception ex)
        {
            _logger.LogError("RefreshCurrenciesFromCurrencyData Exception {Ex}", ex.ToString());
            rs.AddSystemError(ex.ToString());
            return rs;
        }
    }
}


