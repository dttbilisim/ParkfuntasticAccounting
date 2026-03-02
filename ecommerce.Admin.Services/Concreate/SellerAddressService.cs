using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using ecommerce.Admin.Domain.Dtos.SellerAddressDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Domain.Concreate
{
    public class SellerAddressService : ISellerAddressService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<SellerAddress> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public SellerAddressService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _repository = context.GetRepository<SellerAddress>();
            _mapper = mapper;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task<IActionResult<List<SellerAddressListDto>>> GetSellerAddresses(int sellerId)
        {
            var response = new IActionResult<List<SellerAddressListDto>> { Result = new List<SellerAddressListDto>() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerAddress>();
                var addresses = await repo.GetAll(true)
                    .Include(s => s.City)
                    .Include(s => s.Town)
                    .Where(s => s.SellerId == sellerId)
                    .OrderByDescending(s => s.IsDefault)
                    .ThenByDescending(s => s.Id)
                    .ToListAsync();

                response.Result = _mapper.Map<List<SellerAddressListDto>>(addresses);
                // Manual mapping for StockWhereIs
                foreach (var item in response.Result)
                {
                    var entity = addresses.FirstOrDefault(x => x.Id == item.Id);
                    if (entity != null)
                    {
                        item.StockWhereIs = entity.StockWhereIs;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSellerAddresses Exception {Message}", ex);
                response.AddSystemError(ex.Message);
            }
            return response;
        }

        public async Task<IActionResult<SellerAddressUpsertDto>> GetSellerAddressById(int id)
        {
            var rs = new IActionResult<SellerAddressUpsertDto> { Result = new SellerAddressUpsertDto() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerAddress>();
                var address = await repo.GetFirstOrDefaultAsync(
                    predicate: f => f.Id == id,
                    include: q => q
                        .Include(s => s.City)
                        .Include(s => s.Town));
                if (address == null)
                {
                    rs.AddError("Adres bulunamadı");
                    return rs;
                }
                rs.Result = _mapper.Map<SellerAddressUpsertDto>(address);
                rs.Result.StockWhereIs = address.StockWhereIs; // Manual mapping to ensure it returns
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetSellerAddressById Exception {Message}", ex);
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<int>> UpsertSellerAddress(AuditWrapDto<SellerAddressUpsertDto> model)
        {
            var response = new IActionResult<int> { Result = 0 };
            try
            {
                var dto = model.Dto;
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                var repo = uow.GetRepository<SellerAddress>();
                
                // If setting as default, unset other defaults
                if (dto.IsDefault)
                {
                    var otherDefaults = await repo.GetAll(false)
                        .Where(x => x.SellerId == dto.SellerId && x.IsDefault && x.Id != dto.Id)
                        .ToListAsync();
                    foreach (var other in otherDefaults)
                    {
                        other.IsDefault = false;
                        repo.Update(other);
                    }
                }
                
                if (!dto.Id.HasValue)
                {
                    var entity = _mapper.Map<SellerAddress>(dto);
                    entity.Status = 1;
                    entity.CreatedDate = DateTime.Now;
                    entity.ModifiedDate = DateTime.Now;
                    entity.CreatedId = model.UserId;
                    entity.ModifiedId = model.UserId;
                    await repo.InsertAsync(entity);
                    response.Result = entity.Id;
                }
                else
                {
                    var current = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == dto.Id,
                        disableTracking: false);
                    if (current == null)
                    {
                        response.AddError("Adres bulunamadı");
                        return response;
                    }
                    current.CityId = dto.CityId;
                    current.TownId = dto.TownId;
                    current.Address = dto.Address;
                    current.Email = dto.Email;
                    current.PhoneNumber = dto.PhoneNumber;
                    current.Title = dto.Title;
                    current.StockWhereIs = dto.StockWhereIs;
                    current.IsDefault = dto.IsDefault;
                    current.ModifiedDate = DateTime.Now;
                    current.ModifiedId = model.UserId;
                    repo.Update(current); // Restoring explicit update to force state change
                    response.Result = current.Id;
                }

                await uow.SaveChangesAsync();
                var last = uow.LastSaveChangesResult;
                if (last.IsOk)
                {
                    response.AddSuccess("Başarılı");
                    return response;
                }
                if (last.Exception != null)
                {
                    response.AddError(last.Exception.ToString());
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertSellerAddress Exception {Message}", ex);
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> DeleteSellerAddress(AuditWrapDto<SellerAddressDeleteDto> model)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                await uow.DbContext.SellerAddresses.Where(f => f.Id == model.Dto.Id)
                    .ExecuteDeleteAsync();
                await uow.SaveChangesAsync();
                var last = uow.LastSaveChangesResult;
                if (last.IsOk)
                {
                    rs.AddSuccess("Başarılı");
                    return rs;
                }
                if (last.Exception != null)
                {
                    rs.AddError(last.Exception.ToString());
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteSellerAddress Exception {Message}", ex);
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
