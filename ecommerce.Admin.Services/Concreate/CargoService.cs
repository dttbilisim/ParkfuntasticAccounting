using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CargoDto;
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
using Npgsql;
namespace ecommerce.Admin.Domain.Concreate
{
    public class CargoService : ICargoService
    {
        private const string MENU_NAME = "cargoes";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Core.Entities.Cargo> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<CargoListDto> _radzenPagerService;

        public CargoService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<CargoListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<Core.Entities.Cargo>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }

        public async Task<IActionResult<Empty>> DeleteCargo(AuditWrapDto<CargoDeleteDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {


                //Deleted Mark with audit
                await _context.DbContext.Cargoes.Where(f => f.Id == model.Dto.Id).
                    ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int)EntityStatus.Deleted).
                    SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));

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
                _logger.LogError("DeleteCargo Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<CargoUpsertDto>> GetCargoById(int Id)
        {
            var rs = new IActionResult<CargoUpsertDto>
            {
                Result = new()
            };
            try
            {
                var data = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<CargoUpsertDto>(data);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (ObjectDisposedException)
            {
                // Circuit/scope disposed (e.g. user navigated away or connection lost); avoid using disposed DbContext.
                rs.AddError("Bağlantı kapatıldı. Lütfen sayfayı yenileyip tekrar deneyin.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargoById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<CargoListDto>>>> GetCargoes(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<CargoListDto>>> response = new() { Result = new() };

            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status != (int)EntityStatus.Deleted);
                var mapped = _mapper.Map<List<CargoListDto>>(datas);
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
            catch (ObjectDisposedException)
            {
                response.AddError("Bağlantı kapatıldı. Lütfen sayfayı yenileyip tekrar deneyin.");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargoes Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<List<CargoListDto>>> GetCargoes()
        {
            var rs = new IActionResult<List<CargoListDto>>
            {
                Result = new List<CargoListDto>()
            };
            try
            {
                var datas = await _repository.GetAllAsync(predicate: f => f.Status == 1);
                var mapped = _mapper.Map<List<CargoListDto>>(datas);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        rs.Result = mapped;

                }
                return rs;
            }
            catch (ObjectDisposedException)
            {
                rs.AddError("Bağlantı kapatıldı. Lütfen sayfayı yenileyip tekrar deneyin.");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetCargos Exception " + ex.ToString());

                rs.AddError("Liste Al?namad?");
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<int>> UpsertCargo(AuditWrapDto<CargoUpsertDto> model)
        {
            var rs = new IActionResult<int>();

            try
            {
                var dto = model.Dto;
                if (!dto.Id.HasValue)
                {
                    // Check for duplicate name globally
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Name.ToLower() == dto.Name.ToLower() && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kargo firması zaten mevcut.");
                        return rs;
                    }

                    var entity = _mapper.Map<Core.Entities.Cargo>(dto);
                    entity.Status = dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                    await _context.SaveChangesAsync();

                    rs.Result = entity.Id;
                }
                else
                {
                    // Check for duplicate name globally (excluding current entity)
                    var duplicate = await _repository.GetAll(ignoreQueryFilters: true)
                        .AnyAsync(c => c.Id != dto.Id && c.Name.ToLower() == dto.Name.ToLower() && c.Status != (int)EntityStatus.Deleted);
                    if (duplicate)
                    {
                        rs.AddError($"'{dto.Name}' isimli kargo firması zaten mevcut.");
                        return rs;
                    }

                    await _context.DbContext.Cargoes.Where(f => f.Id == model.Dto.Id).
                        ExecuteUpdateAsync(s => s
                        .SetProperty(a => a.Name, dto.Name)
                        .SetProperty(a => a.Amount, dto.Amount)
                        .SetProperty(a => a.CargoOverloadPrice, dto.CargoOverloadPrice)
                        .SetProperty(a => a.CargoType, dto.CargoType)
                        .SetProperty(a => a.CoveredKm, dto.CoveredKm)
                        .SetProperty(a => a.PricePerExtraKm, dto.PricePerExtraKm)
                        .SetProperty(a => a.Status, (dto.StatusBool ? (int)EntityStatus.Active : (int)EntityStatus.Passive))
                        .SetProperty(a => a.ModifiedDate, DateTime.Now)
                        .SetProperty(a => a.ModifiedId, model.UserId).SetProperty(x=>x.Message,dto.Message));

                }
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Kayıt İşlemi Başarılı");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                    {
                        if (lastResult.Exception is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                        {
                            rs.AddError($"'{dto.Name}' isimli kargo firması zaten mevcut.");
                        }
                        else
                        {
                            rs.AddError("Bir hata oluştu. Lütfen tekrar deneyiniz.");
                        }
                    }
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertCargo Exception " + ex.ToString());
                if (ex is DbUpdateException dbEx && dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    rs.AddError($"'{model.Dto.Name}' isimli kargo firması zaten mevcut.");
                }
                else
                {
                    rs.AddSystemError(ex.ToString());
                }
                return rs;
            }
        }
    }
}
