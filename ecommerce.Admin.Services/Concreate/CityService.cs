using AutoMapper;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Admin.Domain.Concreate
{
using Microsoft.Extensions.DependencyInjection; // Add this

    public class CityService : ICityService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<City> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory; // Add this

        public CityService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory serviceScopeFactory) // Update constructor
        {
            _context = context;
            _repository = context.GetRepository<City>();
            _mapper = mapper;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory; // Init
        }

        public async Task<IActionResult<List<CityListDto>>> GetCities()
        {
            IActionResult<List<CityListDto>> response = new() { Result = new() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var cities = await dbContext.City.AsNoTracking().OrderBy(c => c.Name).ToListAsync(); // AsNoTracking is better for read-only lists in new scope
                    var mappedCats = _mapper.Map<List<CityListDto>>(cities);
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
                _logger.LogError("GetCities Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
    }
}
