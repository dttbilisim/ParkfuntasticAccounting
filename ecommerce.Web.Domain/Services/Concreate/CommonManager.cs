using AutoMapper;
using ecommerce.Web.Domain.Email;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Dtos;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.SupportLine;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Web.Domain.Services.Concreate;

public class CommonManager : ICommonManager
{
    private readonly IUnitOfWork<ApplicationDbContext> context;
    private readonly IMapper mapper;
    private readonly IEmailTemplateService _emailTemplate;

    public CommonManager(IUnitOfWork<ApplicationDbContext> _context, IMapper _mapper, IEmailTemplateService emailTemplate)
    {
        context = _context;
        mapper = _mapper;
        _emailTemplate = emailTemplate;
    }

    public async Task<IActionResult<List<CityListDto>>> GetCategoryList()
    {
        var rs = OperationResult.CreateResult<List<CityListDto>>();
        try
        {
            var cities = await context.GetRepository<City>().GetAllAsync(predicate: null);
            var mappedcities = mapper.Map<List<CityListDto>>(cities);
            if (mappedcities == null) return rs;
            if (mappedcities.Count > 0)
            {
                rs.Result = mappedcities;
            }
            else
            {
                rs.AddError("City Listesi Alınamadı");
            }

            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<TownListDto>>> GetTownList()
    {
        var rs = OperationResult.CreateResult<List<TownListDto>>();

        try
        {
            var towns = await context.GetRepository<Town>().GetAllAsync(predicate: null);
            if (towns == null || !towns.Any())
            {
                rs.AddError("İlçe listesi bulunamadı.");
                return rs;
            }

            var mappedTowns = mapper.Map<List<TownListDto>>(towns);
            rs.Result = mappedTowns;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError("İlçe listesi alınırken bir hata oluştu: " + ex.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<BannerItem>>> GetBannerListAsync()
    {
        var rs = OperationResult.CreateResult<List<BannerItem>>();
        try
        {
            var now = DateTime.Now;

            var bannerData = await context.GetRepository<BannerItem>().GetAllAsync(
                predicate: x =>
                    x.Status == 1 &&
                    x.Banner.Status == 1 &&
                    (x.StartDate == null || x.StartDate <= now) &&
                    (x.EndDate == null || now <= x.EndDate),
                include: x => x
                    .Include(y => y.Banner)
                    .Include(y => y.BannerSubItems)
            );

            rs.Result = bannerData.ToList();
        }
        catch (Exception ex)
        {
            rs.AddSystemError("Banner listesi alınırken bir hata oluştu: " + ex.Message);
        }

        return rs;
    }

    public async Task<IActionResult<bool>> BannerCount(BannerCountDto model)
    {
        var rs = OperationResult.CreateResult<bool>();
        try
        {
            await context.DbContext.BannerItems.Where(x => x.Id == model.BannerSubId)
                .ExecuteUpdateAsync(x => x.SetProperty(item => item.BannerCount, +1));
            await context.SaveChangesAsync();
            rs.Result = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return rs;
    }
    public async Task<IActionResult<bool>> SubmitSupportLineAsync(SupportLine entity)
    {
        var rs = OperationResult.CreateResult<bool>();

        try
        {
            entity.CreatedDate = DateTime.Now;
            entity.Status = 1;
           

            await context.GetRepository<SupportLine>().InsertAsync(entity);
            await context.SaveChangesAsync();

            try
            {
                await _emailTemplate.SendSupportLineEmailAsync(entity);
            }
            catch (Exception mailEx)
            {
                rs.AddWarning("Talep kaydedildi ancak e-posta gönderilemedi: " + mailEx.Message);
            }

            rs.Result = true;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError("Destek talebi kaydedilirken bir hata oluştu: " + ex.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<FrequentlyAskedQuestion>>> GetFrequentlyAskedQuestions()
    {
        var rs = OperationResult.CreateResult<List<FrequentlyAskedQuestion>>();
        try
        {
            var asked = await context.GetRepository<FrequentlyAskedQuestion>().GetAllAsync(predicate:x=>x.Status==1 && x.Group == SSSAndBlogGroup.SupportLine);
            rs.Result = asked.ToList();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return rs;
    }

    // === Garage dropdown data ===
    public async Task<IActionResult<List<CarBrand>>> GetCarBrandsAsync()
    {
        var rs = OperationResult.CreateResult<List<CarBrand>>();
        try
        {
            var list = await context.DbContext.Set<CarBrand>()
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (list == null || list.Count == 0)
            {
                rs.Result = new List<CarBrand>();
                rs.AddWarning("Marka bulunamadı.");
                return rs;
            }

            rs.Result = list;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError("Marka listesi alınırken hata oluştu: " + e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<CarModel>>> GetCarModelsByBrandIdAsync(int brandId)
    {
        var rs = OperationResult.CreateResult<List<CarModel>>();
        try
        {
            var list = await context.DbContext.Set<CarModel>()
                .AsNoTracking()
                .Where(x => x.Status == 1 && x.CarBrandId == brandId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (list == null || list.Count == 0)
            {
                rs.Result = new List<CarModel>();
                rs.AddWarning("Bu markaya ait model bulunamadı.");
                return rs;
            }

            rs.Result = list;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError("Model listesi alınırken hata oluştu: " + e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<CarEngine>>> GetCarEnginesByModelIdAsync(int modelId)
    {
        var rs = OperationResult.CreateResult<List<CarEngine>>();
        try
        {
            var list = await context.DbContext.Set<CarEngine>()
                .AsNoTracking()
                .Where(x => x.Status == 1 && x.CarModelId == modelId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (list == null || list.Count == 0)
            {
                rs.Result = new List<CarEngine>();
                rs.AddWarning("Bu modele ait motor bulunamadı.");
                return rs;
            }

            rs.Result = list;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError("Motor listesi alınırken hata oluştu: " + e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<CarFuelType>>> GetCarFuelTypesAsync()
    {
        var rs = OperationResult.CreateResult<List<CarFuelType>>();
        try
        {
            var list = await context.DbContext.Set<CarFuelType>()
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .OrderBy(x => x.Name)
                .ToListAsync();

            if (list == null || list.Count == 0)
            {
                rs.Result = new List<CarFuelType>();
                rs.AddWarning("Yakıt türü bulunamadı.");
                return rs;
            }

            rs.Result = list;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError("Yakıt türü listesi alınırken hata oluştu: " + e.Message);
            return rs;
        }
    }

    public async Task<IActionResult<List<CarYear>>> GetCarYearsAsync()
    {
        var rs = OperationResult.CreateResult<List<CarYear>>();
        try
        {
            var list = await context.DbContext.Set<CarYear>()
                .AsNoTracking()
                .Where(x => x.Status == 1)
                .OrderByDescending(x => x.RawText)
                .ToListAsync();

            if (list == null || list.Count == 0)
            {
                rs.Result = new List<CarYear>();
                rs.AddWarning("Yıl listesi bulunamadı.");
                return rs;
            }

            rs.Result = list;
            return rs;
        }
        catch (Exception e)
        {
            rs.AddSystemError("Yıl listesi alınırken hata oluştu: " + e.Message);
            return rs;
        }
    }
}