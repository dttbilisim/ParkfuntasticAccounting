using AutoMapper;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Extensions;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ecommerce.Core.Entities.Authentication;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.AspNetCore.Identity;
namespace ecommerce.Admin.Domain.Concreate{
    public class CompanyService : ICompanyService{
        private const string MENU_NAME = "sellers";
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<Company> _repository;
        private readonly IRepository<PharmacyData> _pharmacyRepository;
        private readonly IRepository<ProductSellerItem> _productSellerItemRepo;
        private readonly IRepository<ApplicationUser> _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<CompanyListDto> _radzenPagerService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;

        public CompanyService(IUnitOfWork<ApplicationDbContext> context, IMapper mapper, ILogger logger, IRadzenPagerService<CompanyListDto> radzenPagerService, UserManager<ApplicationUser> userManager, ecommerce.Admin.Domain.Services.IPermissionService permissionService){
            _context = context;
            _repository = context.GetRepository<Company>();
            _userRepository = context.GetRepository<ApplicationUser>();
            _pharmacyRepository = context.GetRepository<PharmacyData>();
            _productSellerItemRepo = context.GetRepository<ProductSellerItem>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _userManager = userManager;
            _permissionService = permissionService;
        }
        public async Task<IActionResult<Empty>> DeleteCompany(AuditWrapDto<CompanyDeleteDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                //company tablosunda pasife aliyoruz
                await _context.DbContext.Company.Where(f => f.Id == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, (int) EntityStatus.Deleted).SetProperty(a => a.DeletedDate, DateTime.Now).SetProperty(a => a.DeletedId, model.UserId));
                //kullanici tablosunda pasife aliyoruz.
                await _context.DbContext.AspNetUsers.Where(f => f.CompanyId == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.LockoutEnabled, false));
                //ilanlarini pasife aliyoruz.
                await _context.DbContext.ProductSellerItems.Where(f => f.CompanyId == model.Dto.Id).ExecuteUpdateAsync(s => s.SetProperty(a => a.Status, 99));
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if(lastResult.IsOk){
                    rs.AddSuccess("Successfull");
                    return rs;
                } else{
                    if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            } catch(Exception ex){
                _logger.LogError("DeleteCompany Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Paging<List<CompanyListDto>>>> GetCompanies(PageSetting pager){
            var response = OperationResult.CreateResult<Paging<List<CompanyListDto>>>();
            try{
                response.Result = await _repository.GetAll(true).ToPagedResultAsync<CompanyListDto>(pager, _mapper);
            } catch(Exception e){
                _logger.LogError("GetCompanies Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<List<CompanyListDto>>> GetCompanies(){
            var rs = new IActionResult<List<CompanyListDto>>{Result = new List<CompanyListDto>()};
            try{
                var categories = _repository.GetAll(true);
                var mappedCats = _mapper.Map<List<CompanyListDto>>(categories);
                if(mappedCats != null){
                    if(mappedCats.Count > 0) rs.Result = mappedCats;
                } else
                    rs.AddError("Şirket Listesi Alınamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCompanies Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<CompanyListDto>>> GetCompanies(List<int> Ids){
            var rs = new IActionResult<List<CompanyListDto>>{Result = new List<CompanyListDto>()};
            try{
                var categories = await _repository.GetAllAsync(predicate:x => Ids.Contains(x.Id) && x.CompanyWorkingType == CompanyWorkingType.Seller || x.CompanyWorkingType == CompanyWorkingType.BuyerAndSeller);
                var mappedCats = _mapper.Map<List<CompanyListDto>>(categories);
                if(mappedCats != null){
                    if(mappedCats.Count > 0) rs.Result = mappedCats;
                } else
                    rs.AddError("Şirket Listesi Alınamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCompanies Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<CompanyUpsertDto>> GetCompanyById(int Id){
            var rs = new IActionResult<CompanyUpsertDto>{Result = new()};
            try{
                var company = _repository.GetFirstOrDefault(predicate:f => f.Id == Id);
                var mapped = _mapper.Map<CompanyUpsertDto>(company);
                if(mapped != null){
                    rs.Result = mapped;
                } else
                    rs.AddError("Şirket Bulunamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("Company Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<ReportStorage>>> GetReportList(){
            var rs = new IActionResult<List<ReportStorage>>{Result = new List<ReportStorage>()};
            var response = await _context.DbContext.ReportStorages.ToListAsync();
            rs.Result = response;
            return rs;
        }
        public async Task<IActionResult<Paging<List<PharmacyDataDto>>>> GetPharmacyData(PageSetting pager){
            var response = OperationResult.CreateResult<Paging<List<PharmacyDataDto>>>();
            try{
                response.Result = await _pharmacyRepository.GetAll(predicate:x => x.Status == 1).ToPagedResultAsync<PharmacyDataDto>(pager, _mapper);
            } catch(Exception e){
                _logger.LogError("GetPharmacyData Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<string>> UploadPharmactData(PharmacyData model){
            var response = OperationResult.CreateResult<string>();
            var pharmacyList = await _context.DbContext.PharmacyDatas.AsTracking().ToListAsync();
            var checkPharmacy = pharmacyList.FirstOrDefault(x => x.Email == model.Email);
            if(checkPharmacy == null){
                await _context.DbContext.PharmacyDatas.AddAsync(model);
                response.Result = "insert";
            } else{
                checkPharmacy.StatusText = model.StatusText;
                checkPharmacy.ModifiedDate = DateTime.Now;
                checkPharmacy.ModifiedId = 1;
                _context.DbContext.PharmacyDatas.Update(checkPharmacy);
                response.Result = "update";
            }
            await _context.DbContext.SaveChangesAsync();
            return response;
        }
        public async Task<IActionResult<List<CompanyDocumentListDto>>> GetCompanyDocumentList(string email){
            var rs = new IActionResult<List<CompanyDocumentListDto>>{Result = new List<CompanyDocumentListDto>()};
            try{
                var data = await _context.DbContext.CompanyDocuments.Where(x => x.Email == email && x.Status == 1).ToListAsync();
                var mapped = _mapper.Map<List<CompanyDocumentListDto>>(data);
                if(mapped != null){
                    if(mapped.Count > 0) rs.Result = mapped;
                } else
                    rs.AddError("Data Listesi Alınamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCompanyDocumentList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<CompanyWarehouseListDto>>> GetCompanyWarehouseList(int companyId){
            var rs = new IActionResult<List<CompanyWarehouseListDto>>{Result = new List<CompanyWarehouseListDto>()};
            try{
                var data = await _context.DbContext.CompanyWareHouses.Where(x => x.CompanyId == companyId && x.Status == 1).OrderByDescending(x => x.Id).ToListAsync();
                var mapped = _mapper.Map<List<CompanyWarehouseListDto>>(data);
                if(mapped != null){
                    if(mapped.Count > 0) rs.Result = mapped;
                } else
                    rs.AddError("Data Listesi Alınamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCompanyWarehouseList Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> DeleteCompanyWarehouse(AuditWrapDto<CompanyWarehouseDeleteDto> model){
            var rs = new IActionResult<Empty>{Result = new Empty()};
            try{
                var data = await _context.DbContext.CompanyWareHouses.FirstOrDefaultAsync(x => x.Id == model.Dto.Id);
                if(data != null){
                    _context.DbContext.CompanyWareHouses.Remove(data);
                    await _context.SaveChangesAsync();
                    var lastResult = _context.LastSaveChangesResult;
                    if(lastResult.IsOk){
                        rs.AddSuccess("Silme ??lemi Ba?ar?l?");
                        return rs;
                    } else{
                        if(lastResult != null && lastResult.Exception != null) rs.AddError(lastResult.Exception.ToString());
                        return rs;
                    }
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("DeleteCompanyWarehouse Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<Empty>> UpsertCompanyWarehouse(CompanyWareHouse model){
            var rs = new IActionResult<Empty>{Result = new()};
            try{
                var checkRecord = await _context.DbContext.CompanyWareHouses.FirstOrDefaultAsync(x => x.CompanyId == model.CompanyId && x.CityId == model.CityId && x.TownId == model.TownId);
                if(checkRecord is not null){
                    return rs;
                } else{
                    model.Status = 1;
                    model.CreatedId = 1;
                    model.CreatedDate = DateTime.Now;
                    await _context.DbContext.CompanyWareHouses.AddAsync(model);
                    await _context.SaveChangesAsync();
                    var lastResult = _context.LastSaveChangesResult;
                    if(lastResult.IsOk){
                        rs.AddSuccess("ok");
                    } else{
                        rs.AddError("Kayit Edilemedi bir hata olustu");
                    }
                }
                return rs;
            } catch(Exception ex){
                _logger.LogError("UpsertCompanyWarehouse Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<List<ProductSellerItem>>> GetSellerproducts(int ? sellerId){
            var response = OperationResult.CreateResult<List<ProductSellerItem>>();
            try{
                response.Result = await _productSellerItemRepo.GetAll(predicate:x => x.CompanyId == sellerId).Include(x => x.Product).ThenInclude(x => x.Tax).Include(x => x.Company).ToListAsync();
            } catch(Exception e){
                _logger.LogError("GetPharmacyData Exception " + e);
                response.AddSystemError(e.Message);
            }
            return response;
        }
        public async Task<IActionResult<string>> UpsertCompanyInterview(CompanyInterviewDto model){
            var rs = OperationResult.CreateResult<string>();
            try{
                var map = _mapper.Map<CompanyInterview>(model);
                if(model.Id == 0){
                    await _context.DbContext.CompanyInterviews.AddAsync(map);
                    rs.AddSuccess("Ekleme İşlemi Başarılı");
                } else{
                    await _context.DbContext.CompanyInterviews.Where(x => x.Id == model.Id).ExecuteUpdateAsync(x => x.SetProperty(x => x.Message, model.Message).SetProperty(x => x.Updated, DateTime.Now).SetProperty(x => x.InterviewPersonel, model.InterviewPersonel));
                    rs.AddSuccess("Güncelleme İşlemi Başarılı");
                }
                await _context.SaveChangesAsync();
            } catch(Exception e){
                Console.WriteLine(e);
                rs.AddError(e.Message);
                _logger.LogError(e.Message!);
            }
            return rs;
        }
        public async Task<IActionResult<List<CompanyInterviewDto>>> GetCompanyInterview(int companyId){
            var rs = OperationResult.CreateResult<List<CompanyInterviewDto>>();
            if(companyId == 0){
                rs.AddError("CompanyId 0 Olamaz");
            } else{
                var companyInterview = await _context.GetRepository<CompanyInterview>().GetAllAsync(predicate:x => x.CompanyId == companyId);
                var map = _mapper.Map<List<CompanyInterviewDto>>(companyInterview);
                rs.Result = map;
            }
            return rs;
        }
        public async Task<IActionResult<string>> DeleteCompanyInterView(int Id){
            var rs = OperationResult.CreateResult<string>();
            try{
                await _context.DbContext.CompanyInterviews.Where(x => x.Id == Id).ExecuteDeleteAsync();
                await _context.SaveChangesAsync();
                var lastok = _context.LastSaveChangesResult;
                if(lastok.IsOk){
                    rs.AddSuccess("ok");
                }
            } catch(Exception e){
                Console.WriteLine(e);
                rs.AddError(e.Message);
            }
            return rs;
        }
        public async Task<IActionResult<List<CompanyListDto>>> GetSellerCompanies(){
            var rs = new IActionResult<List<CompanyListDto>>{Result = new List<CompanyListDto>()};
            try{
                var categories = _repository.GetAll(predicate:x => x.CompanyWorkingType == CompanyWorkingType.Seller && x.Status == (int) EntityStatus.Active);
                var mappedCats = _mapper.Map<List<CompanyListDto>>(categories);
                if(mappedCats != null){
                    if(mappedCats.Count > 0) rs.Result = mappedCats;
                } else
                    rs.AddError("Şirket Listesi Alınamadı");
                return rs;
            } catch(Exception ex){
                _logger.LogError("GetCompanies Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
        public async Task<IActionResult<int>> UpsertCompany(AuditWrapDto<CompanyUpsertDto> model){
            var rs = new IActionResult<int>{Result = new()};
            try{
                var strategy = _context.DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () => {
                    using(var transaction = await _context.BeginTransactionAsync()){
                        var dto = model.Dto;
                        if(!dto.Id.HasValue){
                            var existUser = await _userRepository.GetFirstOrDefaultAsync(predicate:x => x.Email == model.Dto.EmailAddress);
                            if(existUser is not null){
                                rs.AddError($"{model.Dto.EmailAddress} Bu email adresine ait kullanıcı bulunmaktadır.");
                                return;
                            }
                            var entity = _mapper.Map<Company>(dto);
                            entity.Status = dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive;
                            entity.CreatedId = model.UserId;
                            entity.CreatedDate = DateTime.Now;
                            entity.IyzicoSubmerhantKey = model.Dto.IyzicoSubmerhantKey;
                            entity.SipaySubmerchantKey = model.Dto.SipaySubmerchantKey;
                            entity.SipayPaymentDay = model.Dto.SipayPaymentDay;
                            entity.SipayCommission = model.Dto.SipayCommission;
                            entity.EntegraXmlLink = model.Dto.EntegraXmlLink;
                            entity.BizimHesapXmlLink = model.Dto.BizimHesapXmlLink;
                            entity.BiFaturaXmlLink = model.Dto.BiFaturaXmlLink;
                            entity.ParasutXmlLink = model.Dto.ParasutXmlLink;
                            entity.ProPazarXmlLink = model.Dto.ProPazarXmlLink;
                            await _repository.InsertAsync(entity);
                            await _context.SaveChangesAsync();
                            rs.Result = entity.Id;
                            var user = new ApplicationUser(){
                                CompanyId = rs.Result,
                                Email = model.Dto.EmailAddress,
                                EmailConfirmed = true,
                                UserName = model.Dto.EmailAddress,
                                FirstName = model.Dto.FirstName,
                                LastName = model.Dto.LastName,
                            };
                            var userManagerResponse = await _userManager.CreateAsync(user, model.Dto.Password);
                            if(userManagerResponse.Succeeded){
                                await transaction.CommitAsync();
                                rs.AddSuccess("Kullanıcı bilgileri oluşturuldu");
                            } else {
                                await transaction.RollbackAsync();
                                rs.AddError("Kullanıcı giriş için bilgiler oluşturulamadı.");
                            }
                        } else{
                            rs.Result = model.Dto.Id!.Value;
                            await _context.DbContext.Company.Where(x => x.Id == model.Dto.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.CompanyWorkingType, model.Dto.CompanyWorkingType).SetProperty(c => c.UserType, model.Dto.UserType).SetProperty(c => c.PharmacyTypeId, model.Dto.PharmacyTypeId).SetProperty(c => c.FirstName, model.Dto.FirstName).SetProperty(c => c.LastName, model.Dto.LastName).SetProperty(c => c.BirthDate, model.Dto.BirthDate).SetProperty(c => c.GlnNumber, model.Dto.GlnNumber).SetProperty(c => c.EmailAddress, model.Dto.EmailAddress).SetProperty(c => c.PhoneNumber, model.Dto.PhoneNumber).SetProperty(c => c.CityId, model.Dto.CityId).SetProperty(c => c.TownId, model.Dto.TownId).SetProperty(c => c.Iban, model.Dto.Iban).SetProperty(c => c.AccountName, model.Dto.AccountName).SetProperty(c => c.TaxNumber, model.Dto.TaxNumber).SetProperty(c => c.TaxName, model.Dto.TaxName).SetProperty(c => c.AccountEmailAddress, model.Dto.AccountEmailAddress).SetProperty(c => c.InvoiceAddress, model.Dto.InvoiceAddress).SetProperty(c => c.Status, dto.StatusBool ? (int) EntityStatus.Active : (int) EntityStatus.Passive).SetProperty(c => c.ModifiedId, model.UserId).SetProperty(c => c.Rate, model.Dto.Rate).SetProperty(c => c.Address, model.Dto.Address).SetProperty(c => c.IyzicoSubmerhantKey, model.Dto.IyzicoSubmerhantKey).SetProperty(c => c.ModifiedDate, DateTime.Now).SetProperty(c => c.VendorPaymentTransferTime, model.Dto.VendorPaymentTransferTime).SetProperty(c => c.Description, model.Dto.Description).SetProperty(c => c.MinBasketAmount, model.Dto.MinBasketAmount).SetProperty(c => c.MinCartTotal, model.Dto.MinCartTotal).SetProperty(c => c.OnlineVideoLimit, model.Dto.OnlineVideoLimit).SetProperty(c => c.OfflineVideoLimit, model.Dto.OfflineVideoLimit).SetProperty(c => c.IsLocalStorage, model.Dto.IsLocalStorage).SetProperty(c => c.SipayPaymentDay, model.Dto.SipayPaymentDay).SetProperty(c => c.SipaySubmerchantKey, model.Dto.SipaySubmerchantKey).SetProperty(c => c.SipayCommission, model.Dto.SipayCommission).SetProperty(x => x.IsBlueTick, model.Dto.IsBlueTick).SetProperty(x => x.BankAccountName, model.Dto.BankAccountName).SetProperty(x => x.PharmacyName, model.Dto.PharmacyName).SetProperty(x => x.SellerNotes, model.Dto.SellerNotes).SetProperty(x => x.EntegraXmlLink, model.Dto.EntegraXmlLink).SetProperty(x => x.BizimHesapXmlLink, model.Dto.BizimHesapXmlLink).SetProperty(x => x.BiFaturaXmlLink, model.Dto.BiFaturaXmlLink).SetProperty(x => x.ParasutXmlLink, model.Dto.ParasutXmlLink).SetProperty(x => x.ProPazarXmlLink, model.Dto.ProPazarXmlLink));
                            
                            var lastResult = _context.LastSaveChangesResult;
                            if(lastResult.IsOk){
                                var users = _context.DbContext.AspNetUsers.FirstOrDefault(x => x.CompanyId == model.Dto.Id);
                                if (users != null) {
                                    if(model.Dto.Status == 1){
                                        users.LockoutEnabled = true;
                                    } else{
                                        users.LockoutEnabled = false;
                                    }
                                    _context.DbContext.AspNetUsers.Update(users);
                                    await _context.SaveChangesAsync();
                                }
                                if(model.Dto.Status == 0){
                                    await _context.DbContext.ProductSellerItems.Where(x => x.CompanyId == model.Dto.Id).ExecuteUpdateAsync(x => x.SetProperty(c => c.Status, 0));
                                    await _context.SaveChangesAsync();
                                }
                                await transaction.CommitAsync();
                                rs.AddSuccess("Başarılı");
                            } else{
                                await transaction.RollbackAsync();
                                if(lastResult != null && lastResult.Exception != null) rs.AddError("Herhangi bir hata oluştu.");
                            }
                        }
                    }
                });
                return rs;
            } catch(Exception ex){
                _logger.LogError("UpsertCategory Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
