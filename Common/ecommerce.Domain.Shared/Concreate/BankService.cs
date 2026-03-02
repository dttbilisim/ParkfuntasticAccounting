using AutoMapper;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCardDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Extensions;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Domain.Shared.Concreate
{
    /// <summary>Banka Yönetimi servisi. Bank ortaktır (BranchId yok). BankParameter, BankCard, BankCreditCardInstallment, BankCreditCardPrefix şirket/şubeye göre (BranchId) listeleme/güncelleme/ekleme/silme.</summary>
    public class BankService : IBankService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantProvider _tenantProvider;
        
        public BankService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger,
            IServiceScopeFactory scopeFactory,
            ITenantProvider tenantProvider)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _scopeFactory = scopeFactory;
            _tenantProvider = tenantProvider;
        }
        
        /// <summary>Bankalar ortaktır (multi-tenant shared); BranchId filtresi uygulanmaz. Diğer özellikler BranchId'ye göre ayrılır.</summary>
        public async Task<IActionResult<Paging<List<BankListDto>>>> GetBanks(PageSetting pager)
        {
            var response = OperationResult.CreateResult<Paging<List<BankListDto>>>();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<Bank>();
                response.Result = await repo.GetAll(true)
                    .ToPagedResultAsync<BankListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBanks Exception " + e);
                response.AddSystemError(e.Message);
            }
            
            return response;
        }
        
        public async Task<IActionResult<BankUpsertDto>> GetBankById(int id)
        {
            var rs = new IActionResult<BankUpsertDto> { Result = new() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<Bank>();
                var bank = await repo.GetFirstOrDefaultAsync(predicate: f => f.Id == id);
                var mapped = _mapper.Map<BankUpsertDto>(bank);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Banka bulunamadı");
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBankById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        
        public async Task<IActionResult<int>> UpsertBank(AuditWrapDto<BankUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<Bank>();
                if (dto.Id == 0)
                {
                    var entity = _mapper.Map<Bank>(dto);
                    
                    // Fallback for required fields
                    if (string.IsNullOrEmpty(entity.SystemName))
                    {
                        entity.SystemName = entity.Name ?? "Bank-" + Guid.NewGuid().ToString().Substring(0, 4);
                    }
                    if (string.IsNullOrEmpty(entity.LogoPath))
                    {
                        entity.LogoPath = "/images/default-bank.png"; 
                    }

                    entity.CreateDate = DateTime.Now;
                    entity.UpdateDate = DateTime.Now;
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Banka bulunamadı");
                        return response;
                    }
                    
                    _mapper.Map(dto, current);
                    current.UpdateDate = DateTime.Now;
                    repo.Update(current);
                    response.Result = current.Id;
                }
                
                await uow.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBank Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        public async Task<IActionResult<Empty>> DeleteBank(AuditWrapDto<BankDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<Bank>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == model.Dto.Id);
                if (entity != null)
                {
                    repo.Delete(entity);
                    await uow.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Banka bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBank Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }

        /// <summary>Circuit-scoped _context ve _tenantProvider kullanır; sadece mevcut şube (BranchId) kayıtları listelenir.</summary>
        public async Task<IActionResult<Paging<List<BankParameterListDto>>>> GetBankParameters(PageSetting pager, int? bankId = null)
        {
            var response = OperationResult.CreateResult<Paging<List<BankParameterListDto>>>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankParameter>();
                var query = repo.GetAll(true).Where(x => x.BranchId == branchId);

                if (bankId.HasValue)
                {
                    query = query.Where(x => x.BankId == bankId.Value);
                }

                response.Result = await query.ToPagedResultAsync<BankParameterListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBankParameters Exception " + e.ToString());
                response.AddSystemError(e.ToString());
            }

            return response;
        }

        public async Task<IActionResult<BankParameterUpsertDto>> GetBankParameterById(int id)
        {
            var response = OperationResult.CreateResult<BankParameterUpsertDto>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankParameter>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == id && x.BranchId == branchId);
                if (entity != null)
                {
                    response.Result = _mapper.Map<BankParameterUpsertDto>(entity);
                }
                else
                {
                    response.AddError("Banka parametresi bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBankParameterById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<int>> UpsertBankParameter(AuditWrapDto<BankParameterUpsertDto> model)
        {
            var response = OperationResult.CreateResult<int>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankParameter>();
                var dto = model.Dto;

                if (dto.Id == 0)
                {
                    var entity = _mapper.Map<BankParameter>(dto);
                    entity.BranchId = branchId;
                    repo.Insert(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.BranchId == branchId,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Banka parametresi bulunamadı");
                        return response;
                    }

                    _mapper.Map(dto, current);
                    current.BranchId = branchId;
                    repo.Update(current);
                    response.Result = current.Id;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBankParameter Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        public async Task<IActionResult<Empty>> DeleteBankParameter(AuditWrapDto<BankParameterDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankParameter>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == model.Dto.Id && x.BranchId == branchId);
                if (entity != null)
                {
                    repo.Delete(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Banka parametresi bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBankParameter Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }

            return response;
        }

        /// <summary>Sadece mevcut şube (BranchId) kayıtları listelenir.</summary>
        public async Task<IActionResult<Paging<List<BankCardListDto>>>> GetBankCards(PageSetting pager,
            int? bankId = null)
        {
            var response = OperationResult.CreateResult<Paging<List<BankCardListDto>>>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCard>();
                var query = repo.GetAll(true).Include(x => x.Bank).Where(x => x.BranchId == branchId);
                if (bankId.HasValue)
                {
                    query = (IIncludableQueryable<BankCard, Bank>)query.Where(x => x.BankId == bankId.Value);
                }
                
                response.Result = await query.ToPagedResultAsync<BankCardListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBankCards Exception " + e);
                response.AddSystemError(e.Message);
            }
            
            return response;
        }
        
        public async Task<IActionResult<BankCardUpsertDto>> GetBankCardById(int id)
        {
            var rs = new IActionResult<BankCardUpsertDto> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCard>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: f => f.Id == id && f.BranchId == branchId);
                var mapped = _mapper.Map<BankCardUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Banka kartı bulunamadı");
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBankCardById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        
        public async Task<IActionResult<int>> UpsertBankCard(AuditWrapDto<BankCardUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCard>();
                if (dto.Id == 0)
                {
                    var entity = _mapper.Map<BankCard>(dto);
                    entity.BranchId = branchId;
                    entity.CreateDate = DateTime.Now;
                    entity.UpdateDate = DateTime.Now;
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.BranchId == branchId,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Banka kartı bulunamadı");
                        return response;
                    }
                    
                    _mapper.Map(dto, current);
                    current.BranchId = branchId;
                    current.UpdateDate = DateTime.Now;
                    repo.Update(current);
                    response.Result = current.Id;
                }
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBankCard Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        public async Task<IActionResult<Empty>> DeleteBankCard(AuditWrapDto<BankCardDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCard>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == model.Dto.Id && x.BranchId == branchId);
                if (entity != null)
                {
                    repo.Delete(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Banka kartı bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBankCard Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        /// <summary>Sadece mevcut şube (BranchId) kayıtları listelenir.</summary>
        public async Task<IActionResult<Paging<List<BankCreditCardInstallmentListDto>>>> GetBankCreditCardInstallments(
            PageSetting pager, int? creditCardId = null)
        {
            var response = OperationResult.CreateResult<Paging<List<BankCreditCardInstallmentListDto>>>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardInstallment>();
                var query = repo.GetAll(true).Include(x => x.CreditCard).Where(x => x.BranchId == branchId);
                if (creditCardId.HasValue)
                {
                    query = (IIncludableQueryable<BankCreditCardInstallment, BankCard>)query.Where(x =>
                        x.CreditCardId == creditCardId.Value);
                }
                
                response.Result = await query.ToPagedResultAsync<BankCreditCardInstallmentListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBankCreditCardInstallments Exception " + e);
                response.AddSystemError(e.Message);
            }
            
            return response;
        }
        
        public async Task<IActionResult<BankCreditCardInstallmentUpsertDto>> GetBankCreditCardInstallmentById(int id)
        {
            var rs = new IActionResult<BankCreditCardInstallmentUpsertDto> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardInstallment>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: f => f.Id == id && f.BranchId == branchId);
                var mapped = _mapper.Map<BankCreditCardInstallmentUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Taksit bilgisi bulunamadı");
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBankCreditCardInstallmentById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        
        public async Task<IActionResult<int>> UpsertBankCreditCardInstallment(
            AuditWrapDto<BankCreditCardInstallmentUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardInstallment>();
                if (dto.Id == 0)
                {
                    var entity = _mapper.Map<BankCreditCardInstallment>(dto);
                    entity.Id = 0; // Yeni kayıt: Id veritabanı tarafından üretilsin (PK duplicate key hatasını önler)
                    entity.BranchId = branchId;
                    entity.CreateDate = DateTime.Now;
                    entity.UpdateDate = DateTime.Now;
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.BranchId == branchId,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Taksit bilgisi bulunamadı");
                        return response;
                    }
                    
                    _mapper.Map(dto, current);
                    current.BranchId = branchId;
                    current.UpdateDate = DateTime.Now;
                    repo.Update(current);
                    response.Result = current.Id;
                }
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBankCreditCardInstallment Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    response.AddError("Taksit kaydı zaten mevcut veya teknik bir çakışma oluştu. Lütfen listeyi yenileyip tekrar deneyin.");
                else
                    response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        public async Task<IActionResult<Empty>> DeleteBankCreditCardInstallment(
            AuditWrapDto<BankCreditCardInstallmentDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardInstallment>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == model.Dto.Id && x.BranchId == branchId);
                if (entity != null)
                {
                    repo.Delete(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Taksit bilgisi bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBankCreditCardInstallment Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        /// <summary>Sadece mevcut şube (BranchId) kayıtları listelenir.</summary>
        public async Task<IActionResult<Paging<List<BankCreditCardPrefixListDto>>>> GetBankCreditCardPrefixes(
            PageSetting pager, int? creditCardId = null)
        {
            var response = OperationResult.CreateResult<Paging<List<BankCreditCardPrefixListDto>>>();
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardPrefix>();
                var query = repo.GetAll(true).Include(x => x.CreditCard).Where(x => x.BranchId == branchId);
                if (creditCardId.HasValue)
                {
                    query = (IIncludableQueryable<BankCreditCardPrefix, BankCard>)query.Where(x =>
                        x.CreditCardId == creditCardId.Value);
                }
                
                response.Result = await query.ToPagedResultAsync<BankCreditCardPrefixListDto>(pager, _mapper);
            }
            catch (Exception e)
            {
                _logger.LogError("GetBankCreditCardPrefixes Exception " + e);
                response.AddSystemError(e.Message);
            }
            
            return response;
        }
        
        public async Task<IActionResult<BankCreditCardPrefixUpsertDto>> GetBankCreditCardPrefixById(int id)
        {
            var rs = new IActionResult<BankCreditCardPrefixUpsertDto> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardPrefix>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: f => f.Id == id && f.BranchId == branchId);
                var mapped = _mapper.Map<BankCreditCardPrefixUpsertDto>(entity);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("Kart prefix bilgisi bulunamadı");
                
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetBankCreditCardPrefixById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        
        public async Task<IActionResult<int>> UpsertBankCreditCardPrefix(
            AuditWrapDto<BankCreditCardPrefixUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardPrefix>();
                if (dto.Id == 0)
                {
                    var entity = _mapper.Map<BankCreditCardPrefix>(dto);
                    entity.Id = 0; // Yeni kayıt: Id veritabanı tarafından üretilsin (PK duplicate key hatasını önler)
                    entity.BranchId = branchId;
                    entity.CreateDate = DateTime.Now;
                    entity.UpdateDate = DateTime.Now;
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id && x.BranchId == branchId,
                        disableTracking: true);
                    if (current == null)
                    {
                        response.AddError("Kart prefix bilgisi bulunamadı");
                        return response;
                    }
                    
                    _mapper.Map(dto, current);
                    current.BranchId = branchId;
                    current.UpdateDate = DateTime.Now;
                    repo.Update(current);
                    response.Result = current.Id;
                }
                
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertBankCreditCardPrefix Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23505")
                    response.AddError("Kart prefix kaydı zaten mevcut veya teknik bir çakışma oluştu. Lütfen listeyi yenileyip tekrar deneyin.");
                else
                    response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
        
        public async Task<IActionResult<Empty>> DeleteBankCreditCardPrefix(
            AuditWrapDto<BankCreditCardPrefixDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new() };
            try
            {
                var branchId = _tenantProvider.GetCurrentBranchId();
                var repo = _context.GetRepository<BankCreditCardPrefix>();
                var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == model.Dto.Id && x.BranchId == branchId);
                if (entity != null)
                {
                    repo.Delete(entity);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    response.AddError("Kart prefix bilgisi bulunamadı");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteBankCreditCardPrefix Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
            }
            
            return response;
        }
    }
}