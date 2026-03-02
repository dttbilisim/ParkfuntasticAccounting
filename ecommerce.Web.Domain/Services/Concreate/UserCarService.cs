using AutoMapper;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Identity;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ecommerce.Core.Interfaces;

namespace ecommerce.Web.Domain.Services.Concreate;

public class UserCarService: IUserCarService
{
    private readonly IUnitOfWork<ApplicationDbContext>context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ITenantProvider _tenantProvider;
    private readonly SemaphoreSlim _carLock = new(1,1);

    public UserCarService(IUnitOfWork<ApplicationDbContext> _context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ITenantProvider tenantProvider)
    {
        context = _context;
        _httpContextAccessor = httpContextAccessor;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _tenantProvider = tenantProvider;
    }

    public async Task<IActionResult<List<UserCars>>> GetAllUserCarsAsync()
    {
        var rs = OperationResult.CreateResult<List<UserCars>>();
        await _carLock.WaitAsync();
        try
        {
            await using var db = CreateDbContext();
            var userCars = await db.Set<UserCars>()
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .ToListAsync();
            if (userCars == null || !userCars.Any())
            {
                rs.Result = new List<UserCars>();
                rs.AddWarning("Kullanıcı aracı bulunamadı.");
                return rs;
            }
            rs.Result = userCars;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _carLock.Release();
        }

        return rs;
    }

    public async Task<IActionResult<List<UserCars>>> GetAllUserCarsByUserIdAsync(int userId)
    {
        var rs = OperationResult.CreateResult<List<UserCars>>();
        await _carLock.WaitAsync();
        try
        {
            await using var db = CreateDbContext();
            var list = await db.Set<UserCars>()
                .AsNoTracking()
                .Include(x => x.DotManufacturer)
                .Include(x => x.DotBaseModel)
                .Include(x => x.DotSubModel)
                .Include(x => x.DotCarBodyOption)
                .Include(x => x.DotEngineOption)
                .Include(x => x.DotVehicleType) // Yeni eklendi
                .Include(x => x.DotOption) // Yeni eklendi
                .Where(x => x.Status == 1 && x.UserId == userId)
                .ToListAsync();

            rs.Result = list ?? new List<UserCars>();
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
        }
        finally
        {
            _carLock.Release();
        }
        return rs;
    }

    public async Task<IActionResult<List<UserCars>>> GetAllUserCarsForCurrentUserAsync()
    {
        var principal = _httpContextAccessor.HttpContext?.User;

        
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            var fail = OperationResult.CreateResult<List<UserCars>>();
            fail.AddError("Unauthorized");
            return fail;
        }
        return await GetAllUserCarsByUserIdAsync(userId);
    }

    private ApplicationDbContext CreateDbContext()
    {
        var rawCs = _configuration.GetConnectionString("ApplicationDbContext");
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(rawCs)
        {
            KeepAlive = 30,
            MaxPoolSize = 200,
            Multiplexing = false
        };
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(csb.ConnectionString, o =>
            {
                o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
                 .EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null)
                 .CommandTimeout(180);
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .EnableThreadSafetyChecks(false)
            .Options;
        return new ApplicationDbContext(options, _tenantProvider);
    }

    public async Task<IActionResult<UserCars>> UpsertUserCarAsync(UserCars userCar)
    {
        var rs = OperationResult.CreateResult<UserCars>();
        try
        {
            // Yeni bir DbContext kullanarak tracking sorununu çöz
            await using var db = CreateDbContext();
            var repo = db.Set<UserCars>();
            UserCars? entity;

            if (userCar.Id > 0)
            {
                // Mevcut entity'yi bul (AsNoTracking ile)
                entity = await repo.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == userCar.Id && x.Status == 1);

                if (entity == null)
                {
                    rs.AddError("Araç bulunamadı.");
                    return rs;
                }

                // Gelen userCar nesnesini direkt güncelle
                userCar.Status = 1; // Status'u koru
                userCar.CreatedDate = entity.CreatedDate; // Eski CreatedDate'i koru
                userCar.ModifiedDate = DateTime.UtcNow;

                // Entity'yi detach edilmiş olarak işaretle ve güncelle
                db.Entry(userCar).State = EntityState.Modified;
            }
            else
            {
                userCar.Status = 1;
                userCar.CreatedDate = DateTime.UtcNow;
                await repo.AddAsync(userCar);
                entity = userCar;
            }

            await db.SaveChangesAsync();

            // İlişkili verileri yükle (AsNoTracking ile)
            var withIncludes = await repo
                .AsNoTracking()
                .Include(x => x.DotManufacturer)
                .Include(x => x.DotBaseModel)
                .Include(x => x.DotSubModel)
                .Include(x => x.DotCarBodyOption)
                .Include(x => x.DotEngineOption)
                .FirstOrDefaultAsync(x => x.Id == entity.Id);

            rs.Result = withIncludes ?? entity;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
        }
        return rs;
    }

    public async Task<IActionResult<UserCars>> DeleteUserCarAsync(int id)
    {
        var rs = OperationResult.CreateResult<UserCars>();
        try
        {
            var repo = context.GetRepository<UserCars>();
            var entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == id && x.Status == 1);

            if (entity == null)
            {
                rs.AddError("Araç bulunamadı.");
                return rs;
            }

            entity.Status = 99;
            entity.ModifiedDate = DateTime.UtcNow;

            repo.Update(entity);
            await context.SaveChangesAsync();

            rs.Result = entity;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
        }
        return rs;
    }
}