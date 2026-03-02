using AutoMapper;
using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class PharmacyTypeService : IPharmacyTypeService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<PharmacyType> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<PharmacyTypeListDto> _radzenPagerService;

        public PharmacyTypeService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<PharmacyTypeListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<PharmacyType>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }

         

        public async Task<IActionResult<List<PharmacyTypeListDto>>> GetPharmacyTypes()
        {
            var rs = new IActionResult<List<PharmacyTypeListDto>>
            {
                Result = new List<PharmacyTypeListDto>()
            };
            try
            {
                var datas = await _context.GetRepository<PharmacyType>().GetAllAsync(predicate: f => f.Status == 1);
                var mapped = _mapper.Map<List<PharmacyTypeListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPharmacyTypes Exception " + ex.ToString());

                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

       
    }
}
