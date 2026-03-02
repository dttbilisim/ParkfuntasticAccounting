using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CategoryDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Models;
using ecommerce.Admin.Domain.Dtos.AppSettingDto;
using ecommerce.Core.Helpers;
namespace ecommerce.Admin.Domain.Concreate
{
    public class AppSettingService : IAppSettingService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<AppSettings> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public AppSettingService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<CategoryListDto> radzenPagerService)
        {
            _context = context;
            _repository = context.GetRepository<AppSettings>();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult<AppSettings>> GetValue(string key)
        {
            var rs = new IActionResult<AppSettings>
            {
                Result = new()
            };
            try
            {
                var value = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Key == key, disableTracking: true);
                var mapped = _mapper.Map<AppSettings>(value);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("AppSettingService.GetValue Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<AppSettings>>> GetValues(params string[] keys)
        {
            var rs = new IActionResult<List<AppSettings>>
            {
                Result = new()
            };
            try
            {
                var value = _repository.GetAll(predicate: f => keys.Contains(f.Key));
                var mapped = _mapper.Map<List<AppSettings>>(value);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("AppSettingService.GetValue Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<Paging<List<AppSettings>>>> GetSettings(int page, int pageSize)
        {
            var rs = new IActionResult<Paging<List<AppSettings>>>();
            try
            {
                var query = _repository.GetAll(predicate: null);
                var count = query.Count();
                var data = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                rs.Result = new Paging<List<AppSettings>>
                {
                    Data = data,
                    DataCount = count,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }

        public async Task<IActionResult<AppSettingUpsertDto>> GetSettingById(int id)
        {
            var rs = new IActionResult<AppSettingUpsertDto>();
            try
            {
                var setting = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Id == id, disableTracking: true);
                if (setting != null) 
                {
                   rs.Result = _mapper.Map<AppSettingUpsertDto>(setting);
                }
            }
            catch (Exception ex)
            {
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }


        public async Task<IActionResult<AppSettings>> UpsertSetting(AuditWrapDto<AppSettingUpsertDto> settingWrapper)
        {
            var rs = new IActionResult<AppSettings>();
            try
            {
                var setting = settingWrapper.Dto; // Extract DTO
                if (setting.Id > 0)
                {
                    var existing = await _repository.GetFirstOrDefaultAsync(predicate: x => x.Id == setting.Id);
                    if (existing != null)
                    {
                        // Manual mapping or AutoMapper can be used here. For safety:
                        existing.Key = setting.Key;
                        existing.Value = setting.Value;
                        existing.Description = setting.Description ?? existing.Description;
                        _repository.Update(existing);
                        rs.Result = existing;
                    }
                }
                else
                {
                    var newEntity = _mapper.Map<AppSettings>(setting);
                    await _repository.InsertAsync(newEntity);
                    rs.Result = newEntity;
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                rs.AddSystemError(ex.Message);
            }
            return rs;
        }
    }
}
