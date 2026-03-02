using AutoMapper;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
using Microsoft.Extensions.DependencyInjection; // Add this

    public class TownService : ITownService
    {
         private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Town> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // Add this

        public TownService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory) // Update constructor
        {
            _context = context;
            _repository = context.GetRepository<Town>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory; // Init
        }

        public async Task<IActionResult<List<TownListDto>>> GetTownsByCityId(int CityId)
        {
            IActionResult<List<TownListDto>> response = new() { Result = new() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                     var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                     // We can use the repository from the new uow
                     var repo = uow.GetRepository<Town>();
                     // Ensure async and isolated
                     var result = await repo.GetAllAsync(predicate:x=>x.CityId==CityId);
                     var orderedResult = result.OrderBy(x => x.Name).ToList();
                     var mappedCats = _mapper.Map<List<TownListDto>>(orderedResult);
                     if (mappedCats != null)
                     {
                         if (mappedCats.Count > 0)
                             response.Result = mappedCats.ToList();
                     }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTowns Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
