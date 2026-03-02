using AutoMapper;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate{
    public class MembershipService : IMembershipService{
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Membership> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<MembershipListDto> _radzenPagerService;
        public MembershipService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<MembershipListDto> radzenPagerService){
            _context = context;
            _repository = context.GetRepository<Membership>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
        }
        public async Task<IActionResult<Empty>> DeleteMembership(AuditWrapDto<MembershipDeleteDto> model){
            var response = new IActionResult<Empty>{Result = new Empty()};
            try{
                _repository.Delete(model.Dto.Id);
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    response.AddSuccess("Successfull");
                    return response;
                }
                if(lastResult != null && lastResult.Exception != null) response.AddError(lastResult.Exception.ToString());
                return response;
            } catch(Exception ex){
                _logger.LogError($"DeleteMembership Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<Paging<IQueryable<MembershipListDto>>>> GetMembership(PageSetting pager){
            IActionResult<Paging<IQueryable<MembershipListDto>>> response = new(){Result = new()};
            try{
                var products = await _repository.GetAllAsync(predicate:null);
                var mappedCats = _mapper.Map<List<MembershipListDto>>(products);
                if(mappedCats != null){
                    if(mappedCats.Count > 0) response.Result.Data = mappedCats.AsQueryable();
                }
                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);
                if(result.Data != null) result.Data = result.Data.OrderByDescending(x => x.RegisterDate);
                response.Result = result;
                return response;
            } catch(Exception ex){
                _logger.LogError("GetMembershipList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<MembershipListDto>>> GetMembership(){
            IActionResult<List<MembershipListDto>> response = new(){Result = new()};
            try{
                var products = await _repository.GetAllAsync(predicate:f => f.Id != 0);
                var mappedCats = _mapper.Map<List<MembershipListDto>>(products);
                if(mappedCats != null){
                    if(mappedCats.Count > 0) response.Result = mappedCats.ToList();
                }
                return response;
            } catch(Exception ex){
                _logger.LogError("GetMembershipList Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<MembershipUpsertDto>> GetMembershipById(int Id){
            IActionResult<MembershipUpsertDto> response = new IActionResult<MembershipUpsertDto>{Result = new()};
            try{
                var product = await _repository.GetFirstOrDefaultAsync(predicate:f => f.Id == Id);
                var mappedCat = _mapper.Map<MembershipUpsertDto>(product);
                if(mappedCat != null){
                    response.Result = mappedCat;
                } else
                    response.AddError("Membership Bulunamadı");
                return response;
            } catch(Exception ex){
                _logger.LogError("GetMembershipById Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<Empty>> UpsertMembership(AuditWrapDto<MembershipUpsertDto> model){
            var response = new IActionResult<Empty>{Result = new Empty()};
            try{
                
                var dto = model.Dto;
                var entity = _mapper.Map<Membership>(dto);
                if(!dto.Id.HasValue){
                    await _repository.InsertAsync(entity);
                } else{
                    entity = await _context.DbContext.Membership.FirstOrDefaultAsync(x => x.Id == dto.Id);
                    entity.UserType = model.Dto.UserType;
                    entity.PharmacyTypeId = model.Dto.PharmacyTypeId;
                    entity.FirstName = model.Dto.FirstName;
                    entity.LastName = model.Dto.LastName;
                    entity.BirthDate = model.Dto.BirthDate;
                    entity.GlnNumber = model.Dto.GlnNumber;
                    entity.EmailAddress = model.Dto.EmailAddress;
                    entity.PhoneNumber = model.Dto.PhoneNumber;
                    entity.CityId = model.Dto.CityId;
                    entity.TownId = model.Dto.TownId;
                    entity.Iban = model.Dto.Iban;
                    entity.AccountName = model.Dto.AccountName;
                    entity.TaxNumber = model.Dto.TaxNumber;
                    entity.TaxName = model.Dto.TaxName;
                    entity.AccountEmailAddress = model.Dto.AccountEmailAddress;
                    entity.InvoiceAddress = model.Dto.InvoiceAddress;
                    entity.RegisterDate = model.Dto.RegisterDate;
                    entity.SendEmail = model.Dto.SendEmail;
                    entity.CompanyWorkingType = model.Dto.CompanyWorkingType;
                    entity.BankAccountName = model.Dto.BankAccountName;
                    _repository.Update(entity);
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    response.AddSuccess("Successfull");
                    return response;
                } else{
                    if(lastResult != null && lastResult.Exception != null) response.AddError(lastResult.Exception.ToString());
                    return response;
                }
            } catch(Exception ex){
                _logger.LogError($"UpsertMembership Exception {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<List<TownListDto>>> GetTownListGetById(int Id){
            var rs = new IActionResult<List<TownListDto>>{Result = new List<TownListDto>()};
            try{
                var towns = await _context.GetRepository<Town>().GetAllAsync(predicate:x => x.CityId == Id);
                var mappedtowns = _mapper.Map<List<TownListDto>>(towns);
                if(mappedtowns != null){
                    if(mappedtowns.Count > 0)
                        rs.Result = mappedtowns;
                    else
                        rs.AddError("Town Listesi Alınamadı");
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("TownList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<CityListDto>>> GetCityList(){
            var rs = new IActionResult<List<CityListDto>>{Result = new List<CityListDto>()};
            try{
                var cities = await _context.GetRepository<City>().GetAllAsync(predicate:null);
                var mappedcities = _mapper.Map<List<CityListDto>>(cities);
                if(mappedcities != null){
                    if(mappedcities.Count > 0)
                        rs.Result = mappedcities;
                    else
                        rs.AddError("City Listesi Alınamadı");
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("CityList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpsertAspnetUser(ApplicationUser model){
            var response = new IActionResult<Empty>{Result = new Empty()};
            try{
                var result = _context.GetRepository<ApplicationUser>().Insert(model);
                //create MembershipActivation
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    response.AddSuccess("Successfull");
                    return response;
                } else{
                    if(lastResult != null && lastResult.Exception != null) response.AddError(lastResult.Exception.ToString());
                    return response;
                }
            } catch(Exception ex){
                _logger.LogError($"ApplicationUser Exception {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }
        public async Task<IActionResult<MembershipActivation>> GetUserToken(int membershipId){
            IActionResult<MembershipActivation> response = new IActionResult<MembershipActivation>{Result = new MembershipActivation()};
            try{
                var res = await _context.GetRepository<MembershipActivation>().GetFirstOrDefaultAsync(predicate:x => x.MembershipId == membershipId);
                response.Result = res;
                return response;
            } catch(Exception ex){
                _logger.LogError($"ApplicationUser Exception {ex.ToString()}");
            }
            return response;
        }
    }
}
