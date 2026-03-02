using AutoMapper;
using ecommerce.Admin.Domain.Dtos.TaxDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class TaxService : ITaxService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Tax> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        public TaxService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<Tax>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task<IActionResult<List<TaxListDto>>> GetTaxes()
        {
            var rs = new IActionResult<List<TaxListDto>>
            {
                Result = new List<TaxListDto>()
            };
            try
            {
                // Yeni scope oluştur - concurrency sorunlarını önlemek için
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repository = context.GetRepository<Tax>();
                    var categories = await repository.GetAllAsync(predicate: f => f.Status == (int)EntityStatus.Active, disableTracking: true);
                    var mappedCats = _mapper.Map<List<TaxListDto>>(categories);
                    if (mappedCats != null)
                    {
                        if (mappedCats.Count > 0)
                            rs.Result = mappedCats;
                    }
                    else rs.AddError("Vergi Listesi Alınamadı");
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTaxes Exception "+ ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
