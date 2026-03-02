using AutoMapper;
using ecommerce.Admin.Domain.Dtos.InvoiceDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using ecommerce.Core.Entities.Hierarchical;
using System.Security.Claims;

namespace ecommerce.Admin.Services.Concreate;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IRepository<Invoice> _invoiceRepository;
    private readonly IRepository<InvoiceItem> _invoiceItemRepository;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    private readonly IRadzenPagerService<InvoiceListDto> _radzenPagerService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public InvoiceService(
        IUnitOfWork<ApplicationDbContext> context,
        IMapper mapper,
        ILogger logger,
        IRadzenPagerService<InvoiceListDto> radzenPagerService,
        ITenantProvider tenantProvider,
        IServiceScopeFactory scopeFactory,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _invoiceRepository = context.GetRepository<Invoice>();
        _invoiceItemRepository = context.GetRepository<InvoiceItem>();
        _mapper = mapper;
        _logger = logger;
        _radzenPagerService = radzenPagerService;
        _tenantProvider = tenantProvider;
        _scopeFactory = scopeFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult<Paging<IQueryable<InvoiceListDto>>>> GetInvoices(PageSetting pager, int? invoiceTypeId = null)
    {
        IActionResult<Paging<IQueryable<InvoiceListDto>>> response = new() { Result = new() };
        try
        {
            // Query oluştur — AsNoTracking ile tracking çakışmasını önle
            var query = _context.DbContext.Invoices
                .AsNoTracking()
                .Include(x => x.Customer)
                .Include(x => x.InvoiceType)
                .Include(x => x.Currency)
                .Where(x => x.Status != (int)EntityStatus.Deleted)
                .AsQueryable();

            if (invoiceTypeId.HasValue && invoiceTypeId.Value > 0)
                query = query.Where(x => x.InvoiceTypeId == invoiceTypeId.Value);

            // Role-based filtering - clean ve maintainable
            query = ApplyInvoiceRoleFilter(query, _context.DbContext);

            // Entity'leri liste olarak çek
            var entities = await query.ToListAsync();

            var mapped = entities
                .Select(x => new InvoiceListDto
                {
                    Id = x.Id,
                    InvoiceNo = x.InvoiceNo,
                    InvoiceSerialNo = x.InvoiceSerialNo,
                    InvoiceDate = x.InvoiceDate,
                    CustomerName = x.CustomerName ?? x.Customer.Name,
                    InvoiceTypeName = x.InvoiceType.Name,
                    TotalAmount = x.TotalAmount,
                    VatTotal = x.VatTotal,
                    GeneralTotal = x.GeneralTotal,
                    TotalAmountCurrency = x.TotalAmountCurrency,
                    VatTotalCurrency = x.VatTotalCurrency,
                    GeneralTotalCurrency = x.GeneralTotalCurrency,
                    IsEInvoice = x.IsEInvoice,
                    IsEArchive = x.IsEArchive,
                    CurrencyCode = x.Currency?.CurrencyCode,
                    CreatedDate = x.CreatedDate,
                    Ettn = x.Ettn,
                    EInvoiceStatus = x.EInvoiceStatus
                })
                .ToList();

            if (mapped?.Count > 0)
            {
                response.Result.Data = mapped
                    .AsQueryable()
                    .OrderByDescending(x => x.Id);
            }

            response.Result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError("GetInvoices Exception " + ex.ToString());
            response.AddSystemError(ex.ToString());
            return response;
        }
    }

    public async Task<IActionResult<InvoiceUpsertDto>> GetInvoiceById(int id)
    {
        var rs = new IActionResult<InvoiceUpsertDto> { Result = new() };
        
        // using var scope = _scopeFactory.CreateScope();
        // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        var scopedContext = _context;

        try
        {
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            var currentBranchId = _tenantProvider.GetCurrentBranchId();
            var user = _httpContextAccessor.HttpContext?.User;

            var entity = await scopedContext.DbContext.Invoices
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Include(x => x.Items)
                .Include(x => x.Customer)
                .Include(x => x.InvoiceType)
                .Include(x => x.SalesPerson)
                .Include(x => x.Currency)
                .Include(x => x.Order)
                .FirstOrDefaultAsync(x => x.Id == id && x.Status != (int)EntityStatus.Deleted
                    && (isGlobalAdmin ? (currentBranchId == 0 || x.BranchId == currentBranchId) : true));

            if (entity == null)
                return rs;

            if (!isGlobalAdmin)
            {
                var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    var isAllowed = await scopedContext.DbContext.UserBranches
                        .AnyAsync(ub => ub.UserId == userId && ub.BranchId == entity.BranchId && ub.Status == (int)EntityStatus.Active);
                    if (!isAllowed)
                    {
                        rs.AddError("Bu faturayı görme yetkiniz yok.");
                        return rs;
                    }
                }
            }

            var dto = new InvoiceUpsertDto
            {
                Id = entity.Id,
                InvoiceNo = entity.InvoiceNo,
                InvoiceTypeName = entity.InvoiceType?.Name,
                OrderNumber = entity.Order?.OrderNumber,
                InvoiceSerialNo = entity.InvoiceSerialNo,
                InvoiceDate = entity.InvoiceDate,
                InvoiceTypeId = entity.InvoiceTypeId,
                CustomerId = entity.CustomerId,
                CustomerName = entity.CustomerName ?? entity.Customer?.Name,
                CurrencyCode = entity.Currency?.CurrencyCode,
                Warehouse = entity.Warehouse,
                WarehouseId = entity.WarehouseId,
                DocumentType = entity.DocumentType,
                PaymentTypeId = entity.PaymentTypeId,
                CashRegisterId = entity.CashRegisterId,
                PcPosDefinitionId = entity.PcPosDefinitionId,
                SalesPersonId = entity.SalesPersonId,
                CurrencyId = entity.CurrencyId,
                IsVatIncluded = entity.IsVatIncluded,
                IsActive = entity.IsActive,
                IsEInvoice = entity.IsEInvoice,
                IsEArchive = entity.IsEArchive,
                IsCashSale = entity.IsCashSale,
                UseCustomerLastInvoiceAddress = entity.UseCustomerLastInvoiceAddress,
                RiskLimit = entity.RiskLimit,
                RiskLimitText = entity.RiskLimitText,
                CurrentBalance = entity.CurrentBalance,
                LastServiceTotal = entity.LastServiceTotal,
                AverageMaturity = entity.AverageMaturity,
                ExchangeRate = entity.ExchangeRate,
                Discount1 = entity.Discount1,
                Discount2 = entity.Discount2,
                Discount3 = entity.Discount3,
                Discount4 = entity.Discount4,
                Discount5 = entity.Discount5,
                TotalAmount = entity.TotalAmount,
                DiscountTotal = entity.DiscountTotal,
                VatTotal = entity.VatTotal,
                GeneralTotal = entity.GeneralTotal,
                TotalAmountCurrency = entity.TotalAmountCurrency,
                DiscountTotalCurrency = entity.DiscountTotalCurrency,
                VatTotalCurrency = entity.VatTotalCurrency,
                GeneralTotalCurrency = entity.GeneralTotalCurrency,
                Description = entity.Description,
                Ettn = entity.Ettn,
                EInvoiceStatus = entity.EInvoiceStatus,
                BranchId = entity.BranchId,
                Items = entity.Items
                    .Where(i => i.Status != (int)EntityStatus.Deleted)
                    .OrderBy(i => i.Order)
                    .Select(i => new InvoiceItemUpsertDto
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductCode = i.ProductCode,
                        ProductName = i.ProductName,
                        Unit = i.Unit,
                        ProductUnitId = i.ProductUnitId,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        PriceCurrency = i.PriceCurrency,
                        VatRate = i.VatRate,
                        Discount1 = i.Discount1,
                        Discount2 = i.Discount2,
                        Discount3 = i.Discount3,
                        Discount4 = i.Discount4,
                        Discount5 = i.Discount5,
                        Total = i.Total,
                        TotalCurrency = i.TotalCurrency,
                        Order = i.Order
                    }).ToList()
            };

            rs.Result = dto;
            return rs;
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "GetInvoiceById Exception");
             rs.AddSystemError(ex.Message);
             return rs;
        }
    }


    public async Task<IActionResult<Empty>> UpsertInvoice(AuditWrapDto<InvoiceUpsertDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        
        // Defensive fallbacks for required fields
        if (string.IsNullOrWhiteSpace(model.Dto.InvoiceSerialNo)) model.Dto.InvoiceSerialNo = "A";
        if (string.IsNullOrWhiteSpace(model.Dto.CustomerName)) model.Dto.CustomerName = "-";
        if (string.IsNullOrWhiteSpace(model.Dto.InvoiceNo)) model.Dto.InvoiceNo = $"INV{DateTime.Now:yyyyMMddHHmmss}";

        // Yeni scope: modal açıkken yüklenen Invoice aynı request'te tracked kalıyor; Update() ikinci instance eklemeye çalışınca "another instance with the key is already being tracked" hatası oluşuyor. Bu yüzden upsert için ayrı DbContext kullanıyoruz.
        using var scope = _scopeFactory.CreateScope();
        var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();

        var strategy = scopedContext.DbContext.Database.CreateExecutionStrategy();
        
        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await scopedContext.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("=== InvoiceService.UpsertInvoice Transaction STARTED (Scoped) === InvoiceId: {InvoiceId}, OrderId: {OrderId}", model.Dto.Id, model.Dto.OrderId);

                // İş Mantığı - Scoped context ile
                await UpsertInvoiceInternal(model, rs, scopedContext);

                if (!rs.Ok)
                {
                    // Internal metodda hata oluştuysa rollback yap
                     _logger.LogWarning("InvoiceService logic failed, rolling back. Errors: {Errors}", rs.Metadata?.Message);
                    await dbTransaction.RollbackAsync();
                    return;
                }

                // Tüm işlemler başarılı - Transaction commit et
                await dbTransaction.CommitAsync();
                _logger.LogInformation("InvoiceService Transaction COMMITTED successfully - InvoiceId: {InvoiceId}", model.Dto.Id);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                _logger.LogError(ex, "InvoiceService Transaction ROLLED BACK due to exception");
                rs.AddError($"Fatura işlemi sırasında beklenmedik bir hata oluştu: {ex.Message}");
            }
        });

        return rs;
    }

    private async Task UpsertInvoiceInternal(AuditWrapDto<InvoiceUpsertDto> model, IActionResult<Empty> rs, IUnitOfWork<ApplicationDbContext> scopedContext)
    {
        // Security Logic
        var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
        var currentBranchId = _tenantProvider.GetCurrentBranchId();
        var user = _httpContextAccessor.HttpContext?.User;

        if (!isGlobalAdmin)
        {
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var allowedBranchIds = await scopedContext.DbContext.UserBranches
                    .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                    .Select(ub => ub.BranchId)
                    .ToListAsync();

                // Check IF branch being assigned is allowed
                var targetBranchId = model.Dto.BranchId != 0 ? model.Dto.BranchId : currentBranchId;
                if (!allowedBranchIds.Contains(targetBranchId))
                {
                    rs.AddError("Seçilen şubeye fatura kesme yetkiniz yok.");
                    return;
                }

                if (model.Dto.Id.HasValue && model.Dto.Id > 0)
                {
                    var existing = await scopedContext.DbContext.Invoices
                        .AsNoTracking()
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                    
                    if (existing != null && !allowedBranchIds.Contains(existing.BranchId))
                    {
                        rs.AddError("Bu faturayı güncelleme yetkiniz yok.");
                        return;
                    }
                }
            }
        }

        // Create scoped repositories
        var scopedInvoiceRepo = scopedContext.GetRepository<Invoice>();
        var scopedInvoiceItemRepo = scopedContext.GetRepository<InvoiceItem>();

        Invoice entity;

        if (!model.Dto.Id.HasValue)
        {
            // INSERT
            _logger.LogInformation("[UpsertInvoice INSERT] DTO Values - CustomerId: {CustomerId}, CustomerName: {CustomerName}, WarehouseId: {WarehouseId}, Warehouse: {Warehouse}, BranchId: {BranchId}",
                model.Dto.CustomerId, model.Dto.CustomerName, model.Dto.WarehouseId, model.Dto.Warehouse, model.Dto.BranchId);
            
            entity = new Invoice
            {
                InvoiceNo = model.Dto.InvoiceNo,
                InvoiceSerialNo = model.Dto.InvoiceSerialNo,
                InvoiceDate = model.Dto.InvoiceDate,
                InvoiceTypeId = model.Dto.InvoiceTypeId ?? 0,
                CustomerId = model.Dto.CustomerId ?? 0,
                CustomerName = model.Dto.CustomerName,
                Warehouse = model.Dto.Warehouse,
                WarehouseId = model.Dto.WarehouseId,
                DocumentType = model.Dto.DocumentType,
                PaymentTypeId = model.Dto.PaymentTypeId,
                CashRegisterId = model.Dto.CashRegisterId,
                PcPosDefinitionId = model.Dto.PcPosDefinitionId,
                SalesPersonId = model.Dto.SalesPersonId,
                CurrencyId = model.Dto.CurrencyId ?? 0,
                IsVatIncluded = model.Dto.IsVatIncluded,
                IsActive = model.Dto.IsActive,
                IsEInvoice = model.Dto.IsEInvoice,
                IsEArchive = model.Dto.IsEArchive,
                IsCashSale = model.Dto.IsCashSale,
                UseCustomerLastInvoiceAddress = model.Dto.UseCustomerLastInvoiceAddress,
                RiskLimit = model.Dto.RiskLimit,
                RiskLimitText = model.Dto.RiskLimitText,
                CurrentBalance = model.Dto.CurrentBalance,
                LastServiceTotal = model.Dto.LastServiceTotal,
                AverageMaturity = model.Dto.AverageMaturity,
                ExchangeRate = model.Dto.ExchangeRate,
                Discount1 = model.Dto.Discount1,
                Discount2 = model.Dto.Discount2,
                Discount3 = model.Dto.Discount3,
                Discount4 = model.Dto.Discount4,
                Discount5 = model.Dto.Discount5,
                TotalAmount = model.Dto.TotalAmount,
                DiscountTotal = model.Dto.DiscountTotal,
                VatTotal = model.Dto.VatTotal,
                GeneralTotal = model.Dto.GeneralTotal,
                TotalAmountCurrency = model.Dto.TotalAmountCurrency,
                DiscountTotalCurrency = model.Dto.DiscountTotalCurrency,
                VatTotalCurrency = model.Dto.VatTotalCurrency,
                GeneralTotalCurrency = model.Dto.GeneralTotalCurrency,
                Description = model.Dto.Description,
                OrderId = model.Dto.OrderId,
                BranchId = model.Dto.BranchId != 0 ? model.Dto.BranchId : _tenantProvider.GetCurrentBranchId(),
                CreatedDate = DateTime.Now,
                CreatedId = model.UserId,
                Status = (int)EntityStatus.Active
            };

            foreach (var item in model.Dto.Items)
            {
                entity.Items.Add(MapToInvoiceItem(item, model.UserId));
            }

            await scopedInvoiceRepo.InsertAsync(entity);
            
            // İlk kayıt (ID oluşması için)
            await scopedContext.SaveChangesAsync();
            CheckSaveResult("Fatura eklenemedi", rs, scopedContext);
            if (!rs.Ok) return;

             rs.AddSuccess("Fatura kaydedildi.");
        }
        else
        {
            // UPDATE — Tracking çakışmasını önlemek için AsNoTracking ile çek
            // Blazor Server'da DbContext circuit boyunca yaşadığı için
            // aynı entity farklı instance olarak tracked kalabiliyor
            entity = await scopedContext.DbContext.Invoices
                .AsNoTracking()
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
            
            if (entity == null)
            {
                rs.AddError("Güncellenecek fatura bulunamadı.");
                return;
            }

            // CALCULATE TOTALS FROM ITEMS (Backend Validation)
            decimal calcTotalAmountCurrency = 0;
            decimal calcTotalDiscountCurrency = 0;
            decimal calcTotalVatCurrency = 0;
            var isTry = model.Dto.CurrencyId == 1131; // TRY
            
            // Prepare items and calculate logic
            var itemsToInsert = new List<InvoiceItem>();
            foreach (var item in model.Dto.Items)
            {
                // Recalculate Item Totals
                decimal itemVatRate = item.VatRate;
                decimal itemUnitPrice = item.Price;
                decimal itemRate = itemVatRate / 100m;
                
                // Net Unit Price Calculation
                decimal unitPriceNet = itemUnitPrice;
                if (model.Dto.IsVatIncluded)
                {
                    unitPriceNet = itemUnitPrice / (1 + itemRate);
                }

                // Line Gross
                decimal lineGross = item.Quantity * unitPriceNet;
                
                // Line Discount
                decimal lineDiscount = lineGross * (item.Discount1 / 100m);
                
                // Line Net (TotalCurrency)
                decimal lineTotalCurrency = Math.Round(lineGross - lineDiscount, 2);
                
                // Line Vat
                decimal lineVat = lineTotalCurrency * itemRate;

                // Update Accumulators
                calcTotalAmountCurrency += Math.Round(lineGross, 2);
                calcTotalDiscountCurrency += Math.Round(lineDiscount, 2);
                calcTotalVatCurrency += Math.Round(lineVat, 2);

                // Update Item DTO values (so MapToInvoiceItem uses correct ones)
                item.TotalCurrency = lineTotalCurrency;
                item.Total = Math.Round(lineTotalCurrency * (isTry ? 1m : model.Dto.ExchangeRate), 2);
                // Also update PriceCurrency/Price to ensure consistency if needed, but usually Price is input.
                
                var newItem = MapToInvoiceItem(item, model.UserId);
                newItem.InvoiceId = entity.Id;
                itemsToInsert.Add(newItem);
            }

            // Calculate General Total
            decimal calcGeneralTotalCurrency = Math.Round((calcTotalAmountCurrency - calcTotalDiscountCurrency) + calcTotalVatCurrency, 2);

            // Entity property'lerini doğrudan güncelle (SetValues tip uyumsuzluğu sorunu nedeniyle kaldırıldı) - v2
            _logger.LogInformation("[UpsertInvoice UPDATE v2] BEFORE assignment - EntityId: {EntityId}, DTO CustomerId: {DtoCustomerId}, DTO CustomerName: {DtoCustomerName}, DTO WarehouseId: {DtoWarehouseId}, DTO Warehouse: {DtoWarehouse}, Entity CustomerId: {EntCustomerId}, Entity CustomerName: {EntCustomerName}, Entity WarehouseId: {EntWarehouseId}, Entity Warehouse: {EntWarehouse}",
                entity.Id, model.Dto.CustomerId, model.Dto.CustomerName, model.Dto.WarehouseId, model.Dto.Warehouse, entity.CustomerId, entity.CustomerName, entity.WarehouseId, entity.Warehouse);
            
            entity.InvoiceNo = model.Dto.InvoiceNo;
            entity.InvoiceSerialNo = model.Dto.InvoiceSerialNo;
            entity.InvoiceDate = model.Dto.InvoiceDate;
            entity.InvoiceTypeId = model.Dto.InvoiceTypeId ?? 0;
            entity.CustomerId = model.Dto.CustomerId ?? 0;
            entity.CustomerName = model.Dto.CustomerName;
            entity.Warehouse = model.Dto.Warehouse;
            entity.WarehouseId = model.Dto.WarehouseId;
            entity.DocumentType = model.Dto.DocumentType;
            entity.PaymentTypeId = model.Dto.PaymentTypeId;
            entity.CashRegisterId = model.Dto.CashRegisterId;
            entity.PcPosDefinitionId = model.Dto.PcPosDefinitionId;
            entity.SalesPersonId = model.Dto.SalesPersonId;
            entity.CurrencyId = model.Dto.CurrencyId ?? 0;
            entity.IsVatIncluded = model.Dto.IsVatIncluded;
            entity.IsActive = model.Dto.IsActive;
            entity.IsEInvoice = model.Dto.IsEInvoice;
            entity.IsEArchive = model.Dto.IsEArchive;
            entity.IsCashSale = model.Dto.IsCashSale;
            entity.UseCustomerLastInvoiceAddress = model.Dto.UseCustomerLastInvoiceAddress;
            entity.RiskLimit = model.Dto.RiskLimit;
            entity.RiskLimitText = model.Dto.RiskLimitText;
            entity.CurrentBalance = model.Dto.CurrentBalance;
            entity.LastServiceTotal = model.Dto.LastServiceTotal;
            entity.AverageMaturity = model.Dto.AverageMaturity;
            entity.ExchangeRate = model.Dto.ExchangeRate;
            entity.Discount1 = model.Dto.Discount1;
            entity.Discount2 = model.Dto.Discount2;
            entity.Discount3 = model.Dto.Discount3;
            entity.Discount4 = model.Dto.Discount4;
            entity.Discount5 = model.Dto.Discount5;
            entity.Description = model.Dto.Description;
            entity.OrderId = model.Dto.OrderId;
            entity.BranchId = model.Dto.BranchId;
            entity.ModifiedId = model.UserId;
            entity.ModifiedDate = DateTime.Now;

            // Hesaplanan değerler (backend validation)
            entity.TotalAmountCurrency = calcTotalAmountCurrency;
            entity.DiscountTotalCurrency = calcTotalDiscountCurrency;
            entity.VatTotalCurrency = calcTotalVatCurrency;
            entity.GeneralTotalCurrency = calcGeneralTotalCurrency;
            entity.TotalAmount = Math.Round(calcTotalAmountCurrency * (isTry ? 1m : model.Dto.ExchangeRate), 2);
            entity.DiscountTotal = Math.Round(calcTotalDiscountCurrency * (isTry ? 1m : model.Dto.ExchangeRate), 2);
            entity.VatTotal = Math.Round(calcTotalVatCurrency * (isTry ? 1m : model.Dto.ExchangeRate), 2);
            entity.GeneralTotal = Math.Round(calcGeneralTotalCurrency * (isTry ? 1m : model.Dto.ExchangeRate), 2);

            _logger.LogInformation("[UpsertInvoice UPDATE v2] AFTER assignment - Entity CustomerId: {CustomerId}, CustomerName: {CustomerName}, WarehouseId: {WarehouseId}, Warehouse: {Warehouse}",
                entity.CustomerId, entity.CustomerName, entity.WarehouseId, entity.Warehouse);

            // Entity'yi Update ile işaretle — AsNoTracking ile çekildiği için Attach + Modified gerekiyor
            scopedContext.DbContext.Invoices.Update(entity);

            // Update items: delete old, insert new
            // IMPORTANT: Using scopedContext for IgnoreQueryFilters within scope
            var existingItems = scopedContext.DbContext.InvoiceItems.IgnoreQueryFilters().Where(x => x.InvoiceId == model.Dto.Id);
            await existingItems.ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, (int)EntityStatus.Deleted));

            foreach (var newItem in itemsToInsert)
            {
                await scopedInvoiceItemRepo.InsertAsync(newItem);
            }

            await scopedContext.SaveChangesAsync();
            CheckSaveResult("Fatura güncellenemedi", rs, scopedContext);
            if (!rs.Ok) return;

            rs.AddSuccess("Fatura güncellendi.");
        }

        // 3. Customer Account Transaction
        if (entity.CustomerId > 0)
        {
            await CreateCustomerAccountTransactionForInvoice(entity, model.UserId, scopedContext);
            // Transaction kaydı hata verirse ana işlem geri alınmalı mı? Genelde evet.
            // CreateCustomerAccountTransactionForInvoice şu an exception yutuyor, onu değiştirebiliriz veya result kontrolü yapabiliriz.
            // Fakat mevcut yapıda loglayıp devam ediyorduk, kullanıcı "senior" istediği için hata fırlatmalı veya RS'e eklemeli.
            if (scopedContext.LastSaveChangesResult.Exception != null)
            {
                 rs.AddError($"Cari hareket işlenemedi: {scopedContext.LastSaveChangesResult.Exception.Message}");
                 return;
            }
        }

        // 4. Update Orders
        await UpdateRelatedOrders(model.Dto.OrderId, model.Dto.OrderIds, entity.Id, rs, scopedContext);
        if (!rs.Ok) return;
    }

    private InvoiceItem MapToInvoiceItem(InvoiceItemUpsertDto item, int userId)
    {
        return new InvoiceItem
        {
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Unit = item.Unit,
            ProductUnitId = item.ProductUnitId,
            Quantity = item.Quantity,
            Price = item.Price,
            PriceCurrency = item.PriceCurrency,
            VatRate = item.VatRate,
            Discount1 = item.Discount1,
            Discount2 = item.Discount2,
            Discount3 = item.Discount3,
            Discount4 = item.Discount4,
            Discount5 = item.Discount5,
            Total = item.Total,
            TotalCurrency = item.TotalCurrency,
            Order = item.Order,
            CreatedDate = DateTime.Now,
            CreatedId = userId,
            Status = (int)EntityStatus.Active
        };
    }

    private void CheckSaveResult(string errorMessage, IActionResult<Empty> rs, IUnitOfWork<ApplicationDbContext> context)
    {
        var lastResult = context.LastSaveChangesResult;
        if (!lastResult.IsOk)
        {
            var ex = lastResult.Exception;
            rs.AddError($"{errorMessage}. {(ex != null ? $"Hata: {ex.Message}" : "")}");
            if (ex != null) _logger.LogError(ex, errorMessage);
        }
    }

    private async Task UpdateRelatedOrders(int? orderId, List<int>? orderIds, int invoiceId, IActionResult<Empty> rs, IUnitOfWork<ApplicationDbContext> context)
    {
        var orderIdsToUpdate = new HashSet<int>();
        if (orderId.HasValue && orderId.Value > 0) orderIdsToUpdate.Add(orderId.Value);
        if (orderIds != null)
            foreach (var oid in orderIds.Where(o => o > 0)) orderIdsToUpdate.Add(oid);

        if (!orderIdsToUpdate.Any()) return;

        var orderRepo = context.GetRepository<Orders>();
        // Explicitly using GetAllAsync with named arguments to ensure correct overload
        var orders = await orderRepo.GetAllAsync(predicate: o => orderIdsToUpdate.Contains(o.Id), disableTracking: false);

        foreach (var order in orders)
        {
            if (order.InvoiceId != invoiceId)
            {
                order.InvoiceId = invoiceId;
                orderRepo.Update(order);
            }
        }
        
        if (orders.Any())
        {
            await context.SaveChangesAsync();
            CheckSaveResult("Sipariş bağlantıları güncellenemedi", rs, context);
        }
    }

    public async Task<IActionResult<Empty>> DeleteInvoice(AuditWrapDto<InvoiceDeleteDto> model)
    {
        var rs = new IActionResult<Empty> { Result = new Empty() };
        
        // using var scope = _scopeFactory.CreateScope();
        // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
        var scopedContext = _context;

        var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
        var currentBranchId = _tenantProvider.GetCurrentBranchId();
        var user = _httpContextAccessor.HttpContext?.User;

        var invoice = await scopedContext.DbContext.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
        if (invoice == null)
        {
            rs.AddError("Fatura bulunamadı.");
            return rs;
        }

        if (!isGlobalAdmin)
        {
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var isAllowed = await scopedContext.DbContext.UserBranches
                    .AnyAsync(ub => ub.UserId == userId && ub.BranchId == invoice.BranchId && ub.Status == (int)EntityStatus.Active);
                if (!isAllowed)
                {
                    rs.AddError("Bu faturayı silme yetkiniz yok.");
                    return rs;
                }
            }
        }

        var strategy = scopedContext.DbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var dbTransaction = await scopedContext.BeginTransactionAsync();
            try
            {
                // 1. Bağlı Siparişleri Güncelle (InvoiceId = null)
                await scopedContext.DbContext.Orders
                     .IgnoreQueryFilters()
                     .Where(o => o.InvoiceId == model.Dto.Id)
                     .ExecuteUpdateAsync(s => s.SetProperty(o => o.InvoiceId, (int?)null));

                // 2. Fatura Kalemlerini Soft Delete Yap
                await scopedContext.DbContext.InvoiceItems
                     .IgnoreQueryFilters()
                     .Where(x => x.InvoiceId == model.Dto.Id)
                     .ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, (int)EntityStatus.Deleted));

                // 3. Faturayı Soft Delete Yap
                await scopedContext.DbContext.Invoices
                    .IgnoreQueryFilters()
                    .Where(f => f.Id == model.Dto.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Status, (int)EntityStatus.Deleted)
                        .SetProperty(a => a.ModifiedId, model.UserId)
                        .SetProperty(a => a.ModifiedDate, DateTime.Now));

                // 4. Faturaya bağlı cari hesap transaction'larını geri al
                await ReverseCustomerAccountTransactions(
                    scopedContext, 
                    invoiceId: model.Dto.Id, 
                    orderId: null, 
                    reason: "Fatura silindi",
                    userId: model.UserId);

                await scopedContext.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                rs.AddSuccess("Fatura silindi.");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                rs.AddError($"Silme işlemi sırasında hata: {ex.Message}");
                _logger.LogError(ex, "DeleteInvoice Error");
            }
        });

        return rs;
    }

    /// <summary>
    /// Fatura iptal/silme durumunda ilgili cari hesap transaction'larını soft delete yapar
    /// ve ters kayıt (Credit/Debit) oluşturarak bakiyeyi düzeltir.
    /// Sadece faturaya bağlı transaction'ları geri alır.
    /// </summary>
    private async Task ReverseCustomerAccountTransactions(
        IUnitOfWork<ApplicationDbContext> context,
        int? invoiceId,
        int? orderId,
        string reason,
        int userId)
    {
        try
        {
            var transactionRepo = context.GetRepository<CustomerAccountTransaction>();

            // Sadece faturaya bağlı aktif transaction'ları bul
            // Sipariş aşamasında transaction atılmadığı için orderId bazlı arama gerekmez
            var activeTransactions = await transactionRepo.GetAll(
                predicate: t => t.Status == (int)EntityStatus.Active
                    && invoiceId.HasValue && t.InvoiceId == invoiceId.Value,
                disableTracking: false
            ).ToListAsync();

            if (!activeTransactions.Any())
            {
                _logger.LogInformation(
                    "Geri alınacak cari hareket bulunamadı — InvoiceId: {InvoiceId}, OrderId: {OrderId}, Sebep: {Reason}",
                    invoiceId, orderId, reason);
                return;
            }

            foreach (var transaction in activeTransactions)
            {
                // Orijinal transaction'ı soft delete yap
                transaction.Status = (int)EntityStatus.Deleted;
                transaction.ModifiedDate = DateTime.Now;
                transaction.ModifiedId = userId;

                // Ters kayıt oluştur — bakiyeyi düzelt
                var reverseType = transaction.TransactionType == CustomerAccountTransactionType.Debit
                    ? CustomerAccountTransactionType.Credit
                    : CustomerAccountTransactionType.Debit;

                // Mevcut bakiyeyi hesapla
                var currentBalance = await transactionRepo.GetAll(
                    predicate: t => t.CustomerId == transaction.CustomerId
                        && t.Status == (int)EntityStatus.Active
                        && t.Id != transaction.Id, // Soft delete edilen hariç
                    disableTracking: true
                ).SumAsync(t => t.TransactionType == CustomerAccountTransactionType.Debit ? t.Amount : -t.Amount);

                // Ters kayıt sonrası bakiye
                var balanceAfterReverse = reverseType == CustomerAccountTransactionType.Credit
                    ? currentBalance - transaction.Amount
                    : currentBalance + transaction.Amount;

                var reverseTransaction = new CustomerAccountTransaction
                {
                    CustomerId = transaction.CustomerId,
                    InvoiceId = transaction.InvoiceId,
                    OrderId = transaction.OrderId,
                    TransactionType = reverseType,
                    Amount = transaction.Amount,
                    TransactionDate = DateTime.Now,
                    Description = $"İPTAL — {reason} (Orijinal: {transaction.Description})",
                    PaymentTypeId = transaction.PaymentTypeId,
                    CashRegisterId = transaction.CashRegisterId,
                    ReferenceNo = transaction.ReferenceNo,
                    BalanceAfterTransaction = balanceAfterReverse,
                    BranchId = transaction.BranchId ?? _tenantProvider.GetCurrentBranchId(),
                    Status = (int)EntityStatus.Active,
                    CreatedDate = DateTime.Now,
                    CreatedId = userId
                };

                await transactionRepo.InsertAsync(reverseTransaction);

                _logger.LogInformation(
                    "Cari hareket geri alındı — TransactionId: {TransactionId}, Tip: {ReverseType}, Tutar: {Amount}, Sebep: {Reason}",
                    transaction.Id, reverseType, transaction.Amount, reason);
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "ReverseCustomerAccountTransactions hatası — InvoiceId: {InvoiceId}, OrderId: {OrderId}, Sebep: {Reason}",
                invoiceId, orderId, reason);
            throw;
        }
    }

    private async Task CreateCustomerAccountTransactionForInvoice(Invoice invoice, int userId, IUnitOfWork<ApplicationDbContext> context)
    {
        try
        {
            var transactionRepo = context.GetRepository<CustomerAccountTransaction>();
            
            // Fatura kesildiğinde HER ZAMAN BORÇ at.
            // Sipariş aşamasında cari hesaba transaction atılmadığı için mükerrer borç kontrolü gerekmez.
            
            // Faturanın ödeme tipini kontrol et
            // Eğer ödeme tipi Veresiye (Cari Hesap) DEĞİLSE → BORÇ at
            // Eğer ödeme tipi Veresiye ise → sipariş oluşturulurken zaten borç atılmış olmalı
            bool isVeresiye = false;
            if (invoice.PaymentTypeId.HasValue)
            {
                // PaymentType entity'sinden kontrol et — IsCash=false ve IsCreditCard=false ise veresiye
                var paymentType = await context.DbContext.Set<ecommerce.Core.Entities.Accounting.PaymentType>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(pt => pt.Id == invoice.PaymentTypeId.Value);
                
                if (paymentType != null && !paymentType.IsCash && !paymentType.IsCreditCard)
                {
                    isVeresiye = true;
                }
            }
            
            // Mevcut bakiyeyi hesapla
            var existingTransactions = await transactionRepo.GetAll(
                predicate: t => t.CustomerId == invoice.CustomerId && t.Status == (int)EntityStatus.Active,
                disableTracking: true
            ).ToListAsync();
            
            var currentBalance = existingTransactions
                .Where(t => t.TransactionType == CustomerAccountTransactionType.Debit)
                .Sum(t => t.Amount) - 
                existingTransactions
                .Where(t => t.TransactionType == CustomerAccountTransactionType.Credit)
                .Sum(t => t.Amount);
            
            // Transaction tipini belirle:
            // Veresiye → BORÇ (müşteri borçlanıyor, fatura karşılığı)
            // Nakit/Kredi Kartı/POS → BORÇ (fatura kesildi, ödeme ayrı takip edilir)
            var transactionType = CustomerAccountTransactionType.Debit;
            var balanceAfterTransaction = currentBalance + invoice.GeneralTotal;
            
            // PaymentTypeId'yi enum'a çevir (Invoice'da int?, CustomerAccountTransaction'da enum)
            ecommerce.Core.Utils.PaymentType? paymentTypeEnum = null;
            if (invoice.PaymentTypeId.HasValue)
            {
                if (invoice.PaymentTypeId.Value == 1)
                    paymentTypeEnum = ecommerce.Core.Utils.PaymentType.CreditCart;
                else if (invoice.PaymentTypeId.Value == 2)
                    paymentTypeEnum = ecommerce.Core.Utils.PaymentType.CustomerBalance;
            }
            
            var transaction = new CustomerAccountTransaction
            {
                CustomerId = invoice.CustomerId,
                InvoiceId = invoice.Id,
                OrderId = invoice.OrderId,
                TransactionType = transactionType,
                Amount = invoice.GeneralTotal,
                TransactionDate = invoice.InvoiceDate,
                Description = $"Fatura: {invoice.InvoiceNo}",
                PaymentTypeId = paymentTypeEnum,
                CashRegisterId = invoice.CashRegisterId,
                ReferenceNo = invoice.InvoiceNo,
                BalanceAfterTransaction = balanceAfterTransaction,
                BranchId = invoice.BranchId,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.Now,
                CreatedId = userId
            };
            
            await transactionRepo.InsertAsync(transaction);
            await context.SaveChangesAsync();
            
            _logger.LogInformation(
                "Fatura {InvoiceId} için cari hareket oluşturuldu — Tip: {TransactionType}, Tutar: {Amount}, Bakiye: {Balance}, Veresiye: {IsVeresiye}", 
                invoice.Id, transactionType, invoice.GeneralTotal, balanceAfterTransaction, isVeresiye);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCustomerAccountTransactionForInvoice Exception — InvoiceId: {InvoiceId}", invoice.Id);
            throw; 
        }
    }

        public async Task<IActionResult<List<InvoiceListDto>>> GetCustomerInvoices(int customerId)
        {
            var result = new IActionResult<List<InvoiceListDto>> 
            { 
                Result = new List<InvoiceListDto>() 
            };

            // using var scope = _scopeFactory.CreateScope();
            // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var scopedContext = _context;

            try
            {
                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var isCustomerB2B = _tenantProvider.IsCustomerB2B;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                List<int> allowedBranchIds = new();
                if (!isGlobalAdmin && !isCustomerB2B && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int userId))
                    {
                         allowedBranchIds = scopedContext.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }
                }

                var invoices = await scopedContext.DbContext.Invoices
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(i => i.CustomerId == customerId && i.Status == (int)EntityStatus.Active
                        && (
                             (isGlobalAdmin || isCustomerB2B) ? (currentBranchId == 0 || i.BranchId == currentBranchId) :
                             (allowedBranchIds.Contains(i.BranchId) && (currentBranchId == 0 || i.BranchId == currentBranchId))
                        ))
                    .Include(i => i.InvoiceType)
                    .Include(i => i.Currency)
                    .Include(i => i.Order)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.Id)
                    .ToListAsync();

                var dtos = invoices.Select(i => new InvoiceListDto
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceSerialNo = i.InvoiceSerialNo,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.CustomerName,
                    InvoiceTypeName = i.InvoiceType?.Name ?? string.Empty,
                    TotalAmount = i.TotalAmount,
                    VatTotal = i.VatTotal,
                    GeneralTotal = i.GeneralTotal,
                    TotalAmountCurrency = i.TotalAmountCurrency,
                    VatTotalCurrency = i.VatTotalCurrency,
                    GeneralTotalCurrency = i.GeneralTotalCurrency,
                    IsEInvoice = i.IsEInvoice,
                    IsEArchive = i.IsEArchive,
                    CurrencyCode = i.Currency?.CurrencyCode,
                    CreatedDate = i.CreatedDate,
                    OrderId = i.OrderId,
                    OrderNumber = i.Order?.OrderNumber
                }).ToList();
                
                // Faturaya bağlı TÜM siparişleri çek (birden fazla sipariş aynı faturaya bağlanabilir)
                var invoiceIds = dtos.Select(d => d.Id).ToList();
                var linkedOrders = await scopedContext.DbContext.Orders
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(o => o.InvoiceId.HasValue && invoiceIds.Contains(o.InvoiceId.Value) 
                        && o.Status != (int)EntityStatus.Deleted)
                    .Select(o => new { o.InvoiceId, o.Id, o.OrderNumber })
                    .ToListAsync();
                
                // Her faturaya bağlı siparişleri grupla ve DTO'ya set et
                var ordersByInvoice = linkedOrders.GroupBy(o => o.InvoiceId!.Value)
                    .ToDictionary(g => g.Key, g => g.Select(o => new InvoiceOrderLinkDto 
                    { 
                        OrderId = o.Id, 
                        OrderNumber = o.OrderNumber 
                    }).ToList());
                
                foreach (var dto in dtos)
                {
                    if (ordersByInvoice.TryGetValue(dto.Id, out var orders))
                    {
                        dto.LinkedOrders = orders;
                    }
                }
                
                result.Result = dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetCustomerInvoices Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }


        public async Task<IActionResult<List<InvoiceListDto>>> GetPlasiyerCustomersInvoices(int userId)
        {
            var result = new IActionResult<List<InvoiceListDto>> 
            { 
                Result = new List<InvoiceListDto>() 
            };

            // using var scope = _scopeFactory.CreateScope();
            // var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var scopedContext = _context;

            try
            {
                 // 1. Get Plasiyer
                var userRepo = scopedContext.GetRepository<ApplicationUser>();
                var appUser = await userRepo.GetFirstOrDefaultAsync(predicate: u => u.Id == userId);

                if (appUser == null || !appUser.SalesPersonId.HasValue) 
                    return result;

                // 2. Get Linked Customers
                var customerIds = await scopedContext.DbContext.CustomerPlasiyers
                    .AsNoTracking()
                    .Where(cp => cp.SalesPersonId == appUser.SalesPersonId.Value)
                    .Select(cp => cp.CustomerId)
                    .ToListAsync();

                if (!customerIds.Any()) return result;

                var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
                var currentBranchId = _tenantProvider.GetCurrentBranchId();
                var user = _httpContextAccessor.HttpContext?.User;

                List<int> allowedBranchIds = new();
                if (!isGlobalAdmin && user != null)
                {
                    var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdClaim, out int loggedInUserId))
                    {
                         allowedBranchIds = scopedContext.DbContext.UserBranches
                            .AsNoTracking()
                            .Where(ub => ub.UserId == loggedInUserId && ub.Status == (int)EntityStatus.Active)
                            .Select(ub => ub.BranchId)
                            .ToList();
                    }
                }

                var invoices = await scopedContext.DbContext.Invoices
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(i => customerIds.Contains(i.CustomerId) && i.Status == (int)EntityStatus.Active
                        && (
                             isGlobalAdmin ? (currentBranchId == 0 || i.BranchId == currentBranchId) :
                             (allowedBranchIds.Contains(i.BranchId) && (currentBranchId == 0 || i.BranchId == currentBranchId))
                        ))
                    .Include(i => i.InvoiceType)
                    .Include(i => i.Currency)
                    .Include(i => i.Order)
                    .OrderByDescending(i => i.InvoiceDate)
                    .ThenByDescending(i => i.Id)
                    .Take(250)
                    .ToListAsync();

                var dtos = invoices.Select(i => new InvoiceListDto
                {
                    Id = i.Id,
                    InvoiceNo = i.InvoiceNo,
                    InvoiceSerialNo = i.InvoiceSerialNo,
                    InvoiceDate = i.InvoiceDate,
                    CustomerName = i.CustomerName,
                    InvoiceTypeName = i.InvoiceType?.Name ?? string.Empty,
                    TotalAmount = i.TotalAmount,
                    VatTotal = i.VatTotal,
                    GeneralTotal = i.GeneralTotal,
                    TotalAmountCurrency = i.TotalAmountCurrency,
                    VatTotalCurrency = i.VatTotalCurrency,
                    GeneralTotalCurrency = i.GeneralTotalCurrency,
                    IsEInvoice = i.IsEInvoice,
                    IsEArchive = i.IsEArchive,
                    CurrencyCode = i.Currency?.CurrencyCode,
                    CreatedDate = i.CreatedDate,
                    OrderId = i.OrderId,
                    OrderNumber = i.Order?.OrderNumber
                }).ToList();
                
                // Faturaya bağlı TÜM siparişleri çek (birden fazla sipariş aynı faturaya bağlanabilir)
                var invoiceIds = dtos.Select(d => d.Id).ToList();
                var linkedOrders = await scopedContext.DbContext.Orders
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(o => o.InvoiceId.HasValue && invoiceIds.Contains(o.InvoiceId.Value) 
                        && o.Status != (int)EntityStatus.Deleted)
                    .Select(o => new { o.InvoiceId, o.Id, o.OrderNumber })
                    .ToListAsync();
                
                var ordersByInvoice = linkedOrders.GroupBy(o => o.InvoiceId!.Value)
                    .ToDictionary(g => g.Key, g => g.Select(o => new InvoiceOrderLinkDto 
                    { 
                        OrderId = o.Id, 
                        OrderNumber = o.OrderNumber 
                    }).ToList());
                
                foreach (var dto in dtos)
                {
                    if (ordersByInvoice.TryGetValue(dto.Id, out var orders))
                    {
                        dto.LinkedOrders = orders;
                    }
                }
                
                result.Result = dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetPlasiyerCustomersInvoices Exception: {ex}");
                result.AddSystemError(ex.ToString());
            }

            return result;
        }

        #region Private Helper Methods for Role-Based Filtering

        /// <summary>
        /// Invoice için role bazlı filtre uygular
        /// </summary>
        private IQueryable<Invoice> ApplyInvoiceRoleFilter(IQueryable<Invoice> query, ApplicationDbContext dbContext)
        {
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            var isB2BAdmin = _tenantProvider.IsB2BAdmin;
            var isPlasiyer = _tenantProvider.IsPlasiyer;
            var isCustomerB2B = _tenantProvider.IsCustomerB2B;
            var currentBranchId = _tenantProvider.GetCurrentBranchId();

            if (isGlobalAdmin)
            {
                return ApplyAdminInvoiceFilter(query, currentBranchId);
            }
            else if (isB2BAdmin)
            {
                return ApplyB2BAdminInvoiceFilter(query, dbContext, currentBranchId);
            }
            else if (isPlasiyer)
            {
                return ApplyPlasiyerInvoiceFilter(query, dbContext);
            }
            else if (isCustomerB2B)
            {
                return ApplyCustomerB2BInvoiceFilter(query, dbContext);
            }

            return query.Where(x => false);
        }

        private IQueryable<Invoice> ApplyAdminInvoiceFilter(IQueryable<Invoice> query, int branchId)
        {
            if (branchId == 0)
                return query; // Tüm kayıtlar (mevcut 0’lı kayıtlar dahil)
            return query.Where(x => x.BranchId == branchId || x.BranchId == 0);
        }

        private IQueryable<Invoice> ApplyB2BAdminInvoiceFilter(IQueryable<Invoice> query, ApplicationDbContext dbContext, int currentBranchId)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return query.Where(x => false);
            }

            var allowedBranchIds = dbContext.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                .Select(ub => ub.BranchId)
                .ToList();

            if (!allowedBranchIds.Any())
            {
                return query.Where(x => false);
            }

            // Kullanıcının yetkili olduğu tüm şubelerin faturalarını göster (seçili şube dropdown’ı sadece varsayılanı belirler, listeyi kısıtlamaz)
            if (currentBranchId > 0)
            {
                return allowedBranchIds.Contains(currentBranchId)
                    ? query.Where(x => x.BranchId == currentBranchId || x.BranchId == 0)
                    : query.Where(x => false);
            }

            return query.Where(x => x.BranchId == 0);
        }

        private IQueryable<Invoice> ApplyPlasiyerInvoiceFilter(IQueryable<Invoice> query, ApplicationDbContext dbContext)
        {
            var salesPersonId = _tenantProvider.GetSalesPersonId();

            if (!salesPersonId.HasValue)
            {
                return query.Where(x => false);
            }

            // Plasiyer'e atanan müşterilerin faturalarını göster
            var assignedCustomerIds = dbContext.CustomerPlasiyers
                .AsNoTracking()
                .Where(cp => cp.SalesPersonId == salesPersonId.Value)
                .Select(cp => cp.CustomerId)
                .ToList();

            return query.Where(x => assignedCustomerIds.Contains(x.CustomerId));
        }

        private IQueryable<Invoice> ApplyCustomerB2BInvoiceFilter(IQueryable<Invoice> query, ApplicationDbContext dbContext)
        {
            var customerId = _tenantProvider.GetCustomerId();

            if (!customerId.HasValue)
            {
                return query.Where(x => false);
            }

            // Sadece kendi faturalarını görebilir
            return query.Where(x => x.CustomerId == customerId.Value);
        }

        #endregion

        /// <summary>
        /// e-Fatura gönderimi sonrası ETTN, durum ve fatura tipi bilgisini günceller
        /// </summary>
        public async Task<IActionResult<Empty>> UpdateEInvoiceStatus(int invoiceId, string ettn, string status, bool isEInvoice, bool isEArchive, int userId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // Doğrudan SQL ile güncelle — tracking ve query filter sorunlarını bypass et
                using var scope = _scopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();

                var rowsAffected = await scopedContext.DbContext.Database.ExecuteSqlRawAsync(
                    @"UPDATE ""Invoices"" SET ""Ettn"" = {0}, ""EInvoiceStatus"" = {1}, ""IsEInvoice"" = {2}, ""IsEArchive"" = {3}, ""ModifiedDate"" = {4}, ""ModifiedId"" = {5} WHERE ""Id"" = {6}",
                    ettn, status, isEInvoice, isEArchive, DateTime.Now, userId, invoiceId);

                if (rowsAffected == 0)
                {
                    rs.AddError("Fatura bulunamadı veya güncellenemedi.");
                }
                
                // e-Fatura iptal edildiğinde cari hesap transaction'larını geri al
                if (status == "IPTAL" && rowsAffected > 0)
                {
                    await ReverseCustomerAccountTransactions(
                        scopedContext,
                        invoiceId: invoiceId,
                        orderId: null,
                        reason: "e-Fatura iptal edildi",
                        userId: userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "e-Fatura durum güncelleme hatası. InvoiceId: {InvoiceId}", invoiceId);
                rs.AddError($"e-Fatura durum güncelleme hatası: {ex.Message}");
            }

            return rs;
        }

        /// <summary>
        /// e-Fatura iptal sonrası ETTN, durum ve fatura tipi bilgilerini temizler
        /// </summary>
        public async Task<IActionResult<Empty>> ClearEInvoiceStatus(int invoiceId, int userId)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                // Yeni scope ile çalış — tracking çakışmasını önle
                using var scope = _scopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var invoiceRepo = scopedContext.GetRepository<Invoice>();

                var invoice = await invoiceRepo.GetFirstOrDefaultAsync(
                    predicate: x => x.Id == invoiceId);

                if (invoice == null)
                {
                    rs.AddError("Fatura bulunamadı.");
                    return rs;
                }

                // ETTN'i koru, durumu "IPTAL" olarak güncelle
                invoice.EInvoiceStatus = "IPTAL";
                invoice.ModifiedDate = DateTime.Now;
                invoice.ModifiedId = userId;

                invoiceRepo.Update(invoice);
                await scopedContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "e-Fatura iptal sonrası güncelleme hatası. InvoiceId: {InvoiceId}", invoiceId);
                rs.AddError($"e-Fatura iptal sonrası güncelleme hatası: {ex.Message}");
            }

            return rs;
        }
    }
