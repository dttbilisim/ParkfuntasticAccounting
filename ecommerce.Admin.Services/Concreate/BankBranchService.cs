using ecommerce.Admin.Domain.Dtos.CheckDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Services.Concreate
{
    /// <summary>
    /// Banka şubesi listesi — tenant bağımsız master data (il/ilçe ile).
    /// </summary>
    public class BankBranchService : IBankBranchService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;

        public BankBranchService(IUnitOfWork<ApplicationDbContext> context)
        {
            _context = context;
        }

        public async Task<IActionResult<List<BankBranchListDto>>> GetList(int? bankId = null, int? cityId = null)
        {
            var result = new IActionResult<List<BankBranchListDto>> { Result = new List<BankBranchListDto>() };

            try
            {
                IQueryable<BankBranch> query = _context.DbContext.BankBranches
                    .AsNoTracking()
                    .Where(x => x.Active)
                    .Include(x => x.Bank)
                    .Include(x => x.City)
                    .Include(x => x.Town);

                if (bankId.HasValue && bankId.Value > 0)
                    query = query.Where(x => x.BankId == bankId.Value);
                if (cityId.HasValue && cityId.Value > 0)
                    query = query.Where(x => x.CityId == cityId.Value);

                var list = await query
                    .OrderBy(x => x.Bank!.Name)
                    .ThenBy(x => x.City!.Name)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

                result.Result = list.Select(x => new BankBranchListDto
                {
                    Id = x.Id,
                    BankId = x.BankId,
                    BankName = x.Bank?.Name ?? "",
                    CityId = x.CityId,
                    CityName = x.City?.Name ?? "",
                    TownId = x.TownId,
                    TownName = x.Town?.Name,
                    Name = x.Name,
                    Code = x.Code,
                    Address = x.Address,
                    Active = x.Active
                }).ToList();
            }
            catch (Exception ex)
            {
                result.AddSystemError(ex.Message);
            }

            return result;
        }
    }
}
