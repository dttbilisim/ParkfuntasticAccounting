using AutoMapper;
using AutoMapper.EquivalencyExpression;
using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCardDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Admin.Domain.Dtos.EmailDto;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Dtos.ProductDto;
using ecommerce.Admin.Domain.Dtos.SellerItemDto;
using ecommerce.Admin.Domain.Dtos.RulesDto;
using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Admin.Domain.Dtos.SearchSynonymDto;
using ecommerce.Admin.Domain.Dtos.SurveyDto;
using ecommerce.Admin.Domain.Dtos.UserMenuDto;
using ecommerce.Admin.Domain.Dtos.RegionDto;
using ecommerce.Admin.Domain.Dtos.SalesPersonDto;
using ecommerce.Admin.Domain.Dtos.UnitDto;
using ecommerce.Admin.Domain.Dtos.ProductUnitDto;
using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Admin.Domain.Dtos.Customer;
using ecommerce.Core.Entities.Warehouse;
using ecommerce.Core.Entities;
using ecommerce.Admin.Domain.Dtos.PaymentTypeDto;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Models;
using ecommerce.Core.Rules;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateSurveyMappings();
        CreateRuleMappings();
        CreateDiscountMappings();
        CreateIdentityMappings();
        CreateProductMappings();
        CreateCompanyMappings();
        CreateSellerMappings();
        CreateUserMappings();
        CreateBankMappings();
        CreateOrderMappings();
        CreateUserMenuMappings();
        CreateRoleMappings();
        CreateMenuMappings();
        CreateCustomerMappings();
        CreateRegionMappings();
        CreatePaymentTypeMappings();
        CreateMonthMappings();
        CreateCustomerWorkPlanMappings();
        CreateUnitMappings();
        CreateProductUnitMappings();
        CreateProductUnitMappings();
        CreateWarehouseMappings();
        CreateCashRegisterMappings();
        CreatePcPosMappings();
        CreateSearchSynonymMappings();
        CreateSellerItemMappings();
        CreateSaleOptionsMappings();
    }

    private void CreateSaleOptionsMappings()
    {
        CreateMap<SaleOptions, ecommerce.Admin.Domain.Dtos.SaleOptionsDto.SaleOptionsListDto>();
        CreateMap<SaleOptions, ecommerce.Admin.Domain.Dtos.SaleOptionsDto.SaleOptionsUpsertDto>().ReverseMap();
    }

    private void CreateWarehouseMappings()
    {        CreateMap<Warehouse, ecommerce.Admin.Domain.Dtos.WarehouseDto.WarehouseListDto>()
            .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForMember(d => d.TownName, opt => opt.MapFrom(s => s.Town != null ? s.Town.Name : null));

        CreateMap<Warehouse, ecommerce.Admin.Domain.Dtos.WarehouseDto.WarehouseUpsertDto>().ReverseMap();
    }

    private void CreateUnitMappings()
    {
        CreateMap<Unit, UnitListDto>();
        CreateMap<Unit, UnitUpsertDto>().ReverseMap();
    }

    private void CreateProductUnitMappings()
    {
        CreateMap<ProductUnit, ProductUnitListDto>();
        CreateMap<ProductUnit, ProductUnitUpsertDto>().ReverseMap();
    }

    private void CreateCustomerMappings()
    {
        CreateMap<Customer, ecommerce.Admin.Domain.Dtos.Customer.CustomerListDto>()
             .ForMember(d => d.CorporationName, opt => opt.MapFrom(s => s.Corporation != null ? s.Corporation.Name : null))
             .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : null))
             .ForMember(d => d.TownName, opt => opt.MapFrom(s => s.Town != null ? s.Town.Name : null))
             .ForMember(d => d.RegionName, opt => opt.MapFrom(s => s.Region != null ? s.Region.Name : null));

        CreateMap<Customer, ecommerce.Admin.Domain.Dtos.Customer.CustomerUpsertDto>()
            .ForMember(d => d.Branches, opt => opt.MapFrom(s => s.CustomerBranches))
            .ReverseMap()
            .ForMember(d => d.CustomerBranches, opt => opt.Ignore())
            .ForMember(d => d.City, opt => opt.Ignore())
            .ForMember(d => d.Town, opt => opt.Ignore())
            .ForMember(d => d.Region, opt => opt.Ignore())
            .ForMember(d => d.Corporation, opt => opt.Ignore())
            .ForMember(d => d.Branch, opt => opt.Ignore());

        CreateMap<CustomerBranch, CustomerBranchUpsertDto>()
             .ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
             .ForMember(d => d.CorporationId, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.CorporationId : 0))
             .ForMember(d => d.CorporationName, opt => opt.MapFrom(s => (s.Branch != null && s.Branch.Corporation != null) ? s.Branch.Corporation.Name : null))
             .ReverseMap()
             .ForMember(d => d.Branch, opt => opt.Ignore())
             .ForMember(d => d.Customer, opt => opt.Ignore());

        CreateMap<InvoiceTypeDefinition, ecommerce.Admin.Domain.Dtos.InvoiceTypeDto.InvoiceTypeListDto>();
        CreateMap<InvoiceTypeDefinition, ecommerce.Admin.Domain.Dtos.InvoiceTypeDto.InvoiceTypeUpsertDto>().ReverseMap();


        CreateMap<SalesPerson, SalesPersonListDto>()
            .ForMember(d => d.BranchName, opt => opt.MapFrom(s => s.Branch != null ? s.Branch.Name : null))
            .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForMember(d => d.TownName, opt => opt.MapFrom(s => s.Town != null ? s.Town.Name : null));
        CreateMap<SalesPerson, SalesPersonUpsertDto>().ReverseMap();
    }

    private void CreateOrderMappings()
    {
        CreateMap<Orders, OrderListDto>()
            .ForMember(d => d.Company, opt => opt.MapFrom(s => (User?)null)) // Admin context'te User null, use CustomerName instead
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => 
                // Priority 1: B2B Customer (Cari) Name from ApplicationUser.Customer
                s.ApplicationUser != null && s.ApplicationUser.Customer != null 
                    ? s.ApplicationUser.Customer.Name 
                    // Priority 2: ApplicationUser FullName (for non-B2B users)
                    : (s.ApplicationUser != null ? s.ApplicationUser.FullName : s.UserFullName ?? ""))) 
            .ForMember(d => d.BuyerName, opt => opt.MapFrom(s => 
                // Buyer Name: UserAddress FullName (delivery contact)
                s.UserAddress != null && !string.IsNullOrWhiteSpace(s.UserAddress.FullName) 
                    ? s.UserAddress.FullName 
                    : (s.UserFullName ?? "")));
    }

    private void CreateUserMappings()
    {
        CreateMap<User, ecommerce.Admin.Domain.Dtos.UserDto.UserListDto>()
            .ForMember(d => d.WebUserType, opt => opt.MapFrom(s => s.WebUserType))
            .ForMember(d => d.IsAproved, opt => opt.MapFrom(s => s.IsAproved));
        CreateMap<User, ecommerce.Admin.Domain.Dtos.UserDto.UserUpsertDto>()
            .ForMember(d => d.WebUserType, opt => opt.MapFrom(s => s.WebUserType))
            .ForMember(d => d.IsAproved, opt => opt.MapFrom(s => s.IsAproved))
            .ReverseMap()
            .ForMember(u => u.UserName, opt => opt.Ignore())
            .ForMember(u => u.PasswordHash, opt => opt.Ignore())
            .ForMember(u => u.WebUserType, opt => opt.MapFrom(s => s.WebUserType))
            .ForMember(u => u.IsAproved, opt => opt.MapFrom(s => s.IsAproved));
        
        CreateMap<User, CompanyUpsertDto>();
    }

    private void CreateSellerMappings()
    {
        CreateMap<Seller, ecommerce.Admin.Domain.Dtos.SellerDto.SellerListDto>()
            .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : null))
            .ForMember(d => d.TownName, opt => opt.MapFrom(s => s.Town != null ? s.Town.Name : null));

        CreateMap<Seller, ecommerce.Admin.Domain.Dtos.SellerDto.SellerUpsertDto>()
            .ReverseMap()
            .ForMember(dest => dest.Town, opt => opt.Ignore());

        CreateMap<Seller, CompanyUpsertDto>()
            .ForMember(d => d.EmailAddress, opt => opt.MapFrom(s => s.Email));

        // SellerAddress Mappings
        CreateMap<SellerAddress, ecommerce.Admin.Domain.Dtos.SellerAddressDto.SellerAddressListDto>()
            .ForMember(d => d.CityName, opt => opt.MapFrom(s => s.City != null ? s.City.Name : string.Empty))
            .ForMember(d => d.TownName, opt => opt.MapFrom(s => s.Town != null ? s.Town.Name : string.Empty));

        CreateMap<SellerAddress, ecommerce.Admin.Domain.Dtos.SellerAddressDto.SellerAddressUpsertDto>()
            .ReverseMap()
            .ForMember(dest => dest.Seller, opt => opt.Ignore())
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.Town, opt => opt.Ignore());
    }

    private void CreateSurveyMappings()
    {
        CreateMap<SurveyOption, SurveyOptionUpsertDto>()
            .ReverseMap()
            .EqualityComparison((dto, m) => dto.Id == m.Id);
    }
    private void CreateCompanyMappings(){
        CreateMap<CompanyInterview, CompanyInterviewDto>().ReverseMap();
      

    }

    private void CreateRuleMappings()
    {
        CreateMap<Rule, RuleUpsertDto>()
            .ForMember(
                r => r.Value,
                exp => exp.MapFrom(
                    d => RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array).Contains(d.Operator) ? null : d.Value
                )
            )
            .ForMember(
                r => r.Values,
                exp => exp.MapFrom(
                    d => RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array).Contains(d.Operator) && d.Value != null
                        ? d.Value.Split(RuleConsts.ExpressionValueArrayDelimiter, StringSplitOptions.None)
                        : null
                )
            )
            .ReverseMap()
            .ForMember(
                r => r.Value,
                exp => exp.MapFrom(
                    d => d.Operator.HasValue && RuleOperatorMapping.GetOperators(RuleExpressionOperatorType.Array).Contains(d.Operator.Value) && d.Values != null
                        ? string.Join(RuleConsts.ExpressionValueArrayDelimiter, d.Values)
                        : d.Value
                )
            );
    }

    private void CreateDiscountMappings()
    {
        CreateMap<Discount, DiscountUpsertDto>().ReverseMap()
            .ForMember(d => d.CompanyCoupons, opt => opt.Ignore());
        CreateMap<Discount, DiscountListDto>();
    }
    
    private void CreateIdentityMappings()
    {
        CreateMap<ApplicationUser, IdentityUserListDto>()
            .ForMember(dest => dest.Roles, op => op.MapFrom(src => src.Roles));

        CreateMap<ApplicationUser, IdentityUserUpsertDto>()
            .ForMember(dest => dest.Roles, op => op.MapFrom(src => src.Roles.Select(r => r.Id).ToList()))
            .ReverseMap()
            .ForMember(m => m.UserName, opt => opt.Ignore())
            .ForMember(m => m.Email, opt => opt.Ignore())
            .ForMember(m => m.PhoneNumber, opt => opt.Ignore())
            .ForMember(m => m.PasswordHash, opt => opt.Ignore())
            .ForMember(m => m.Roles, opt => opt.Ignore());
    }

    private void CreateProductMappings()
    {
        CreateMap<Product, ProductListDto>()
            .ForMember(dest => dest.Kdv, opt => opt.MapFrom(src => src.Tax != null ? src.Tax.TaxRate : (int?)null));
        CreateMap<ProductOnline, ProductOnlineDto>();

        CreateProjection<Product, ProductListForProjectionDto>()
            .ForMember(dest => dest.ProductsWithoutImage,
                       opt => opt.MapFrom(src => !src.ProductImage.Any(x => x.Status == (int)EntityStatus.Active)))
             .ForMember(dest => dest.ProductsWithoutCategory,
                       opt => opt.MapFrom(src => !src.Categories.Any()))
             .ForMember(dest => dest.ProductsImageCount,
                       opt => opt.MapFrom(src => src.ProductImage.Count(x => x.Status == (int)EntityStatus.Active)))
           
            .ForMember(dest=>dest.Category1,
                opt=>opt.MapFrom(src=>src.Categories.Skip(0).Take(1).FirstOrDefault(x=>x.ProductId==src.Id ).Category.Name))
            .ForMember(dest=>dest.Category2,
                opt=>opt.MapFrom(src=>src.Categories.Skip(1).Take(2).FirstOrDefault(x=>x.ProductId==src.Id ).Category.Name))
            
            .ForMember(dest=>dest.Category3,
                opt=>opt.MapFrom(src=>src.Categories.Skip(2).Take(3).FirstOrDefault(x=>x.ProductId==src.Id ).Category.Name))
            
            .ForMember(dest=>dest.Kdv,
                opt=>opt.MapFrom(src=>src.Tax.TaxRate))
            
            .ForMember(dest=>dest.Form,
                opt=>opt.MapFrom(src=>src.ProductType.Name));

        CreateMap<ProductListForProjectionDto, ProductListDto>();

    }

    private void CreateBankMappings()
    {
        CreateMap<Bank, BankListDto>();
        CreateMap<Bank, BankUpsertDto>().ReverseMap()
            .ForMember(d => d.CreateDate, opt => opt.Ignore())
            .ForMember(d => d.UpdateDate, opt => opt.Ignore())
            .ForMember(d => d.Installments, opt => opt.Ignore())
            .ForMember(d => d.CreditCards, opt => opt.Ignore())
            .ForMember(d => d.Parameters, opt => opt.Ignore());

        CreateMap<BankParameter, BankParameterListDto>()
            .ForMember(d => d.BankName, opt => opt.MapFrom(s => s.Bank != null ? s.Bank.Name : null));
        CreateMap<BankParameter, BankParameterUpsertDto>().ReverseMap()
            .ForMember(d => d.Bank, opt => opt.Ignore());

        CreateMap<BankCard, BankCardListDto>()
            .ForMember(d => d.BankName, opt => opt.MapFrom(s => s.Bank != null ? s.Bank.Name : null));
        CreateMap<BankCard, BankCardUpsertDto>().ReverseMap()
            .ForMember(d => d.CreateDate, opt => opt.Ignore())
            .ForMember(d => d.UpdateDate, opt => opt.Ignore())
            .ForMember(d => d.Bank, opt => opt.Ignore())
            .ForMember(d => d.Prefixes, opt => opt.Ignore())
            .ForMember(d => d.Installments, opt => opt.Ignore());

        CreateMap<BankCreditCardInstallment, BankCreditCardInstallmentListDto>()
            .ForMember(d => d.CreditCardName, opt => opt.MapFrom(s => s.CreditCard != null ? s.CreditCard.Name : null));
        CreateMap<BankCreditCardInstallment, BankCreditCardInstallmentUpsertDto>().ReverseMap()
            .ForMember(d => d.CreateDate, opt => opt.Ignore())
            .ForMember(d => d.UpdateDate, opt => opt.Ignore())
            .ForMember(d => d.CreditCard, opt => opt.Ignore());

        CreateMap<BankCreditCardPrefix, BankCreditCardPrefixListDto>()
            .ForMember(d => d.CreditCardName, opt => opt.MapFrom(s => s.CreditCard != null ? s.CreditCard.Name : null));
        CreateMap<BankCreditCardPrefix, BankCreditCardPrefixUpsertDto>().ReverseMap()
            .ForMember(d => d.CreateDate, opt => opt.Ignore())
            .ForMember(d => d.UpdateDate, opt => opt.Ignore())
            .ForMember(d => d.CreditCard, opt => opt.Ignore());
    }

    private void CreateUserMenuMappings()
    {
        CreateMap<UserMenu, ecommerce.Admin.Domain.Dtos.UserMenuDto.UserMenuListDto>()
            .ForMember(d => d.MenuName, opt => opt.MapFrom(s => s.Menu != null ? s.Menu.Name : null))
            .ForMember(d => d.MenuPath, opt => opt.MapFrom(s => s.Menu != null ? s.Menu.Path : null))
            .ForMember(d => d.MenuIcon, opt => opt.MapFrom(s => s.Menu != null ? s.Menu.Icon : null));
        
        CreateMap<ecommerce.Admin.Domain.Dtos.UserMenuDto.UserMenuUpsertDto, UserMenu>().ReverseMap();
    }

    private void CreateMenuMappings()
    {
        CreateMap<Menu, ecommerce.Admin.Domain.Dtos.MenuDto.MenuListDto>()
            .ForMember(d => d.ParentName, opt => opt.MapFrom(s => s.Parent != null ? s.Parent.Name : null));
        
        CreateMap<ecommerce.Admin.Domain.Dtos.MenuDto.MenuUpsertDto, Menu>().ReverseMap();
    }

    private void CreateRoleMappings()
    {
        CreateMap<ApplicationRole, RoleListDto>();
        CreateMap<ApplicationRole, RoleUpsertDto>();
    }

    private void CreateRegionMappings()
    {
        CreateMap<Region, RegionListDto>();
        CreateMap<Region, RegionUpsertDto>().ReverseMap();
    }

    private void CreateMonthMappings()
    {
        CreateMap<Month, ecommerce.Admin.Domain.Dtos.MonthDto.MonthListDto>();
    }

    private void CreateCustomerWorkPlanMappings()
    {
        // CustomerWorkPlanListDto için manuel mapping yapılıyor (service'de)
        // Burada sadece temel mapping varsa eklenebilir
    }

    private void CreatePaymentTypeMappings()
    {
        CreateMap<ecommerce.Core.Entities.Accounting.PaymentType, PaymentTypeListDto>()
            .ForMember(d => d.CurrencyName, opt => opt.MapFrom(s => s.Currency != null ? s.Currency.CurrencyCode : null));
        CreateMap<ecommerce.Core.Entities.Accounting.PaymentType, PaymentTypeUpsertDto>().ReverseMap();
    }

    private void CreateCashRegisterMappings()
    {
        CreateMap<CashRegister, ecommerce.Admin.Domain.Dtos.CashRegisterDto.CashRegisterListDto>()
            .ForMember(d => d.CurrencyCode, opt => opt.MapFrom(s => s.Currency != null ? s.Currency.CurrencyCode : null))
            .ForMember(d => d.PaymentTypeName, opt => opt.MapFrom(s => s.PaymentType != null ? s.PaymentType.Name : null))
            .ForMember(d => d.BranchName, opt => opt.Ignore());

        CreateMap<CashRegister, ecommerce.Admin.Domain.Dtos.CashRegisterDto.CashRegisterUpsertDto>().ReverseMap();
    }

    private void CreatePcPosMappings()
    {
        CreateMap<PcPosDefinition, ecommerce.Admin.Domain.Dtos.PcPosDto.PcPosListDto>()
            .ForMember(d => d.PaymentTypeName, opt => opt.MapFrom(s => s.PaymentType != null ? s.PaymentType.Name : null)); // PaymentType eklendiğinde açılır

        CreateMap<PcPosDefinition, ecommerce.Admin.Domain.Dtos.PcPosDto.PcPosUpsertDto>().ReverseMap();
    }

    private void CreateSearchSynonymMappings()
    {
        CreateMap<SearchSynonym, SearchSynonymListDto>();
        CreateMap<SearchSynonym, SearchSynonymUpsertDto>().ReverseMap();
    }

    private void CreateSellerItemMappings()
    {
        CreateMap<SellerItem, SellerItemListDto>()
            .ForMember(d => d.SellerName, opt => opt.MapFrom(s => s.Seller != null ? s.Seller.Name : null))
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : null))
            .ForMember(d => d.Oem, opt => opt.MapFrom(s => s.Product != null && s.Product.ProductGroupCodes.Any() ? s.Product.ProductGroupCodes.FirstOrDefault().OemCode : null))
            .ForMember(d => d.Barcode, opt => opt.MapFrom(s => s.Product != null ? s.Product.Barcode : null));

        CreateMap<SellerItem, SellerItemUpsertDto>().ReverseMap();
    }
}