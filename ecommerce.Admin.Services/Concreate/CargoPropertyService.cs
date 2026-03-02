using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CargoPropertyDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class CargoPropertyService : ICargoPropertyService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<CargoProperty> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<CargoPropertyListDto> _radzenPagerService;

        public CargoPropertyService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger,
            IRadzenPagerService<CargoPropertyListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<CargoProperty>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }

        public async Task<IActionResult<Empty>> DeleteCargoProperty(AuditWrapDto<CargoPropertyDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                //Deleted Mark with audit
                await _context.DbContext.CargoProperties.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.Status, (int) EntityStatus.Deleted)
                        .SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Silme ??lemi Ba?ar?l?");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteCargoProperty Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<CargoPropertyUpsertDto>> GetCargoPropertyById(int Id)
        {
            var rs = new IActionResult<CargoPropertyUpsertDto>
            {
                Result = new()
            };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<CargoPropertyUpsertDto>(data);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargoPropertyById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<CargoPropertyListDto>>>> GetCargoProperties(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<CargoPropertyListDto>>> response = new() {Result = new()};

            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == (int) EntityStatus.Active);
                var mapped = _mapper.Map<List<CargoPropertyListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        response.Result.Data = mapped.AsQueryable();
                }

                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);

                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargoProperties Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CargoPropertyListDto>>> GetCargoProperties(int cargoId)
        {
            var rs = new IActionResult<List<CargoPropertyListDto>>
            {
                Result = new List<CargoPropertyListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == 1 && f.CargoId == cargoId);
                var mapped = _mapper.Map<List<CargoPropertyListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;
                }

                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargoPropertys Exception " + ex.ToString());

                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertCargoProperty(AuditWrapDto<CargoPropertyUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<CargoProperty>(dto);
                if (!dto.Id.HasValue)
                {
                    entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);

                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.DbContext.CargoProperties.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Size, dto.Size)
                        .SetProperty(a => a.DesiMinValue, dto.DesiMinValue)
                        .SetProperty(a => a.DesiMaxValue, dto.DesiMaxValue)
                        .SetProperty(a => a.Price, dto.Price)
                        .SetProperty(a => a.CargoId, dto.CargoId)
                        .SetProperty(a => a.Status,
                            (dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive))
                        .SetProperty(a => a.ModifiedDate, DateTime.Now)
                        .SetProperty(a => a.ModifiedId, model.UserId));
                }

                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Kayıt Işlemi Başarılı");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertCargoProperty Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}