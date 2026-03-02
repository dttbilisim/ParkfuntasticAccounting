using AutoMapper;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Web.Domain.Dtos.Bank;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Web.Domain.Services.Concreate;

public class BankService : IBankService
{
    private readonly IUnitOfWork<ApplicationDbContext> _uow;
    private readonly ILogger<BankService> _logger;
    private readonly IMapper _mapper;

    public BankService(IUnitOfWork<ApplicationDbContext> uow, ILogger<BankService> logger, IMapper mapper)
    {
        _uow = uow;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IActionResult<List<BankListDto>>> GetActiveBanksAsync()
    {
        var result = OperationResult.CreateResult<List<BankListDto>>();
        try
        {
            var banks = await _uow.GetRepository<Bank>().GetAll(predicate: x => x.Active).ToListAsync();
            var dtos = banks.Select(x => new BankListDto
            {
                Id = x.Id,
                Name = x.Name,
                LogoPath = x.LogoPath,
                SystemName = x.SystemName
            }).ToList();
            
            result.Result = dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveBanksAsync Error");
            result.AddSystemError(ex.Message);
        }
        return result;
    }

    public async Task<IActionResult<List<BankCardListDto>>> GetBankCardsAsync(int bankId)
    {
        var result = OperationResult.CreateResult<List<BankCardListDto>>();
        try
        {
            var cards = await _uow.GetRepository<BankCard>()
                .GetAll(predicate: x => x.BankId == bankId && x.Active && !x.Deleted)
                .ToListAsync();
                
            var dtos = cards.Select(x => new BankCardListDto
            {
                Id = x.Id,
                Name = x.Name,
                BankId = x.BankId
            }).ToList();

            result.Result = dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBankCardsAsync Error");
            result.AddSystemError(ex.Message);
        }
        return result;
    }

    public async Task<IActionResult<List<BankInstallmentListDto>>> GetBankInstallmentsAsync(int cardId)
    {
        var result = OperationResult.CreateResult<List<BankInstallmentListDto>>();
        try
        {
            var installments = await _uow.GetRepository<BankCreditCardInstallment>()
                .GetAll(predicate: x => x.CreditCardId == cardId && x.Active && !x.Deleted)
                .OrderBy(x => x.Installment)
                .ToListAsync();

            var dtos = installments.Select(x => new BankInstallmentListDto
            {
                Id = x.Id,
                Installment = x.Installment,
                InstallmentRate = x.InstallmentRate,
                CreditCardId = x.CreditCardId
            }).ToList();

            result.Result = dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBankInstallmentsAsync Error");
            result.AddSystemError(ex.Message);
        }
        return result;
    }
}
