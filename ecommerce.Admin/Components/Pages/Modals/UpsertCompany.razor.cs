using Blazored.FluentValidation;
using ecommerce.Admin.CustomComponents.Modals;
using ecommerce.Admin.Domain.Dtos.CompanyCargoDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.CompanyRateDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Dtos.PharmacyTypeDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Iyzico.Payment.Interface;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Radzen;
using Radzen.Blazor;
using static ecommerce.Admin.ConfigureValidators.Validations;
using CompanyWareHouse = ecommerce.Core.Entities.CompanyWareHouse;
namespace ecommerce.Admin.Components.Pages.Modals{
    public partial class UpsertCompany{
        #region Injection
        [Inject] protected IJSRuntime JSRuntime{get;set;}
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] protected TooltipService TooltipService{get;set;}
        [Inject] protected ContextMenuService ContextMenuService{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public IConfiguration Configuration{get;set;}
        [Inject] public Microsoft.AspNetCore.Hosting.IHostingEnvironment HostingEnvironment{get;set;}
        [Inject] public ICompanyService CompanyService{get;set;}
        [Inject] public ICityService CityService{get;set;}
        [Inject] public ITownService TownService{get;set;}
        [Inject] public ICompanyRateService CompanyRateService{get;set;}
        [Inject] public IAppSettingService AppSettingService{get;set;}
        [Inject] public IPharmacyTypeService PharmacyTypeService{get;set;}
        [Inject] public INotificationEventService NotificationEventService{get;set;}
        [Inject] public ICompanyCargoService CompanyCargoService{get;set;}
        [Inject] public UserManager<ApplicationUser> UserManager{get;set;}
        [Inject] protected IEmailService _emailService{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        [Inject] protected IPaymentService _paymentService{get;set;}
        [Inject] protected HelperGeneral _helperGeneral{get;set;}
        [Inject] private FileHelper FileHelper{get;set;}
        #endregion
        [Parameter] public int ? Id{get;set;}
        public bool IsShowPharmacyTypes = false;
        public bool IsShowPanels = false;
        private bool IsUserSaved;
        private bool IsSaveButtonDisabled = false;
        protected bool errorVisible;
        protected string Password;
        public List<string> ValidationErrors = new();
        protected CompanyUpsertDto company = new();
        protected List<CityListDto> cities = new();
        protected List<TownListDto> towns = new();
        protected List<CompanyRateListDto> companyRateList = new();
        protected List<CompanyCargoListDto> companyCargoList = new();
        protected List<PharmacyTypeListDto> pharmacyTypes = new();
        protected List<ProductSellerItem> _productSellerItems = new();
        protected List<CompanyInterviewDto> _companyInterview = new();
        protected List<AppSettings> appSettingList = new();
        public string BaseUrl{get;set;}
        private bool IsShowLocalStorage;
        private int CityId, TownId;
        private FluentValidationValidator ? _fluentValidationValidator;
        IEnumerable<CompanyWorkingType> CompanyTypes = Enum.GetValues(typeof(CompanyWorkingType)).Cast<CompanyWorkingType>();
        IEnumerable<UserType> UserTypes = Enum.GetValues(typeof(UserType)).Cast<UserType>();
        public bool Status{get;set;} = true;
        public List<CompanyDocumentListDto> CompanyDocumentList{get;set;}
        public List<CompanyWarehouseListDto> CompanyWarehouseList{get;set;}
        protected RadzenDataGrid<ProductSellerItem> ? radzenDataGridProductSellerItems = new();
        protected override async Task OnInitializedAsync(){
            BaseUrl = Configuration.GetValue<string>("FileUrl") + "CompanyDocuments";
            var appSettingResponse = await AppSettingService.GetValues("NewUserCommissionRate");
            if(appSettingResponse.Ok){
                int value;
                if(int.TryParse(appSettingResponse.Result.FirstOrDefault().Value, out value)) company.Rate = value;
            }
            await GetCities();
            var pharmacyTypeResponse = await PharmacyTypeService.GetPharmacyTypes();
            if(pharmacyTypeResponse.Ok) pharmacyTypes = pharmacyTypeResponse.Result;
            if(Id.HasValue){
                IsShowPanels = true;
                var companySingleRs = await CompanyService.GetCompanyById(Id.Value);
                var companyRateResponse = await CompanyRateService.GetCompanyRateByCompanyId(Id.Value);
                var companyCargoResponse = await CompanyCargoService.GetCompanyCargoes(Id.Value);
                if(companyCargoResponse.Ok) companyCargoList = companyCargoResponse.Result;
                if(companyRateResponse.Ok) companyRateList = companyRateResponse.Result;
                if(companySingleRs.Ok && companySingleRs.Result != null){
                    company = companySingleRs.Result;
                    Status = company.Status != (int) EntityStatus.Passive && company.Status != (int) EntityStatus.Deleted;
                    IsShowPharmacyTypes = company.UserType == UserType.Seller;
                    IsShowLocalStorage = company.UserType == UserType.Seller;
                    StateHasChanged();
                    await LoadCityOfTowns(company.CityId);
                    if(company.Status == (int) EntityStatus.Deleted) IsSaveButtonDisabled = true;
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, companySingleRs.GetMetadataMessages());
                }
                await GetCompanyInterviewList();
            } else{
                IsUserSaved = true;
                company.Password = GeneratePassword();
            }
            var companyDoucmentList = await CompanyService.GetCompanyDocumentList(company.EmailAddress);
            if(companyDoucmentList.Ok){
                CompanyDocumentList = companyDoucmentList.Result;
            }
            if(company.IsLocalStorage){
                await LoadDataCompanyWarehouse();
            }
            await LoadProductSellerItems(Id);
        }
        private async Task LoadProductSellerItems(int ? sellerId){
            var rs = await CompanyService.GetSellerproducts(sellerId);
            if(rs.Ok){
                _productSellerItems = rs.Result;
            }
        }
        private async Task GetCompanyInterviewList(){
            var companyInterviewList = await CompanyService.GetCompanyInterview(Id.Value);
            if(companyInterviewList.Ok) _companyInterview = companyInterviewList.Result;
        }
        private async Task GetCities(){
            var cityResponse = CityService.GetCities();
            if(cityResponse.Result.Ok) cities = cityResponse.Result.Result;
        }
        protected async Task FormSubmit(){
            try{
                company.Id = Id;
                company.StatusBool = Status;
                company.Status = (Status == true ? 1 : 0);
                if(company.CompanyWorkingType == CompanyWorkingType.Seller || company.CompanyWorkingType == CompanyWorkingType.BuyerAndSeller){
                    if(company.IyzicoSubmerhantKey == null){}
                }
                var submitRs = await CompanyService.UpsertCompany(new AuditWrapDto<CompanyUpsertDto>(){UserId = Security.User.Id, Dto = company,});
                if(submitRs.Ok){
                    if(Id.HasValue){
                        DialogService.Close(company);
                    } else{
                        NotificationService.Notify(NotificationSeverity.Success, "Başarılı", submitRs.GetMetadataMessages());
                        //await _emailService.SendNewUserEmail($"{company.FirstName} {company.LastName}", company.EmailAddress, company.Password);
                        Id = submitRs.Result;
                        IsShowPanels = true;
                        IsUserSaved = false;
                    }
                } else{
                    NotificationService.Notify(NotificationSeverity.Error, submitRs.GetMetadataMessages());
                }
            } catch(Exception ex){
                errorVisible = true;
                NotificationService.Notify(NotificationSeverity.Error, ex.ToString());
            }
        }
        protected async Task ShowErrors(){
            var validator = new CompanyUpsertDtoDtoValidator();
            var res = validator.Validate(company);
            ValidationErrors.AddRange(res.Errors.Select(x => x.ErrorMessage));
            List<Dictionary<string, string>> error = await PrepareErrorsForWarningModal(ValidationErrors);
            Dictionary<string, object> param = new();
            param.Add("Errors", error);
            await DialogService.OpenAsync<ValidationModal>("Uyari", param);
            ValidationErrors.Clear();
        }
        private async Task<List<Dictionary<string, string>>> PrepareErrorsForWarningModal(List<string> errors){
            List<Dictionary<string, string>> error = new();
            foreach(var errorText in ValidationErrors){
                Dictionary<string, string> messageDictionary = new Dictionary<string, string>();
                messageDictionary.Add(errorText.Split("-")[0], errorText.Split("-")[1]);
                error.Add(messageDictionary);
            }
            return error;
        }
        protected async Task AddCompanyRateButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertCompanyRate>("Oran Ekle", new Dictionary<string, object>{{"CompanyId", Id.Value},}, new DialogOptions(){Width = "1200px"});
            await LoadDataCompanyRates();
        }
        protected async Task AddCompanyCargoButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertCompanyCargo>("Kargo Ekle", new Dictionary<string, object>{{"SellerId", Id.Value},}, new DialogOptions(){Width = "1200px"});
            await LoadDataCompanyCargo();
        }
        protected async Task EditRowCompanyRate(CompanyRateListDto args){await DialogService.OpenAsync<UpsertCompanyRate>("Oran Düzenle", new Dictionary<string, object>{{"Id", args.Id},{"CompanyId", Id.Value},}, new DialogOptions(){Width = "1200px"});}
        protected async Task EditRowCompanyCargo(CompanyCargoListDto args){
            await DialogService.OpenAsync<UpsertCompanyCargo>("Kargo Düzenle", new Dictionary<string, object>{{"Id", args.Id},{"SellerId", args.CargoId},}, new DialogOptions(){Width = "1200px"});
            await LoadDataCompanyRates();
        }
        protected async Task EditCompanyInterviewButtonClick(CompanyInterviewDto args){
            await DialogService.OpenAsync<UpsertCompanyInterview>("Not Güncelle", new Dictionary<string, object>{{"CompanyId", args.CompanyId},{"Id", args.Id}}, new DialogOptions(){Width = "1200px"});
            await GetCompanyInterviewList();
        }
        protected async Task AddCompanyInterviewButtonClick(){
            await DialogService.OpenAsync<UpsertCompanyInterview>("Not Ekle", new Dictionary<string, object>{{"CompanyId", Id.Value},{"Id", 0}}, new DialogOptions(){Width = "1200px"});
            await GetCompanyInterviewList();
        }
        protected async Task GridDeleteCompanyRateButtonClick(MouseEventArgs args, CompanyRateListDto companyRate){
            try{
                if(await DialogService.Confirm("Seçilen oranı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await CompanyRateService.DeleteCompanyRate(new AuditWrapDto<CompanyRateDeleteDto>(){UserId = Security.User.Id, Dto = new CompanyRateDeleteDto(){Id = companyRate.Id}});
                    if(deleteResult != null){
                        await LoadDataCompanyRates();
                    }
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"});
            }
        }
        protected async Task GridDeleteCompanyCargoButtonClick(MouseEventArgs args, CompanyCargoListDto companyCargo){
            if(await DialogService.Confirm("Seçilen kargoyu silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await CompanyCargoService.DeleteCompanyCargo(new AuditWrapDto<CompanyCargoDeleteDto>(){UserId = Security.User.Id, Dto = new CompanyCargoDeleteDto(){Id = companyCargo.Id}});
                if(deleteResult != null){
                    await LoadDataCompanyCargo();
                    StateHasChanged();
                }
            }
        }
        protected async Task GridDeleteCompanyInterviewButtonClick(int Id){
            if(await DialogService.Confirm("Seçilen kaydı silmek istediğinize emin misiniz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                var deleteResult = await CompanyService.DeleteCompanyInterView(Id);
                if(deleteResult.Metadata.Message.Contains("ok")){
                    await GetCompanyInterviewList();
                    StateHasChanged();
                }
            }
        }
        protected async Task GridDeleteCompanyWarehouseClick(MouseEventArgs args, CompanyWarehouseListDto input){
            var deleteResult = await CompanyService.DeleteCompanyWarehouse(new AuditWrapDto<CompanyWarehouseDeleteDto>(){UserId = Security.User.Id, Dto = new CompanyWarehouseDeleteDto(){Id = input.Id}});
            if(deleteResult != null){
                await LoadDataCompanyWarehouse();
                StateHasChanged();
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Success, Summary = $"Bilgi", Detail = $"Kayıt Silindi."});
            }
        }
        protected async Task CityChange(object value){
            var townResponse = await TownService.GetTownsByCityId((int) value);
            if(townResponse.Ok){
                CityId = Convert.ToInt32(value);
                towns = townResponse.Result;
            }
        }
        protected Task TownChange(object value){
            TownId = Convert.ToInt32(value);
            return Task.CompletedTask;
        }
        protected async Task LoadCityOfTowns(object value){
            var townResponse = await TownService.GetTownsByCityId((int) value);
            if(townResponse.Ok) towns = townResponse.Result;
            StateHasChanged();
        }
        protected async Task LoadDataCompanyRates(){
            var companyRates = await CompanyRateService.GetCompanyRateByCompanyId(Id.Value);
            if(companyRates.Ok) companyRateList = companyRates.Result;
            StateHasChanged();
        }
        protected async Task LoadDataCompanyCargo(){
            var companyCargoResponse = await CompanyCargoService.GetCompanyCargoes(Id.Value);
            if(companyCargoResponse.Ok) companyCargoList = companyCargoResponse.Result;
            StateHasChanged();
        }
        protected async Task LoadDataCompanyWarehouse(){
            var rs = await CompanyService.GetCompanyWarehouseList(Convert.ToInt32(company.Id));
            if(rs.Ok) CompanyWarehouseList = rs.Result;
        }
        private async Task CheckIsLocalWarehouse(bool obj){
            if(obj == true){
                await LoadDataCompanyWarehouse();
            }
        }
        private async Task AddCompanyWarehouseButtonClick(){
            try{
                if(CityId == 0){
                    NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Lütfen İl Seçiniz"});
                    return;
                } else
                    if(TownId == 0){
                        NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Lütfen İlçe Seçiniz"});
                        return;
                    } else{
                        var rs = await CompanyService.UpsertCompanyWarehouse(new CompanyWareHouse{CompanyId = Convert.ToInt32(company.Id), CityId = CityId, TownId = TownId});
                        if(rs.Ok){
                            await LoadDataCompanyWarehouse();
                            NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Success, Summary = $"Bilgi", Detail = $"Kayıt Eklendi."});
                        }
                    }
            } catch(Exception e){
                Console.WriteLine(e);
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Hata olustu"});
            }
        }
        private async Task AddCompanyWarehouseCityButtonClick(){
            try{
                if(CityId == 0){
                    NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Lütfen İl Seçiniz"});
                    return;
                }
                foreach(var item in towns){
                    await CompanyService.UpsertCompanyWarehouse(new CompanyWareHouse{CompanyId = Convert.ToInt32(company.Id), CityId = CityId, TownId = item.Id});
                }
                await LoadDataCompanyWarehouse();
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Success, Summary = $"Bilgi", Detail = $"Seçilen İlin İlçeri Eklendi."});
            } catch(Exception e){
                Console.WriteLine(e);
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Hata olustu"});
            }
        }
        protected void CancelButtonClick(MouseEventArgs args){DialogService.Close(null);}
        protected void ChangeUserType(object value){
            IsShowPharmacyTypes = UserType.Seller == (UserType) value;
            if(UserType.Seller == (UserType) value){
                IsShowLocalStorage = true;
            } else{
                IsShowLocalStorage = false;
                company.IsLocalStorage = false;
            }
        }
        protected string GeneratePassword(){
            var password = "";
            string[] randomChars = new[]{"ABCDEFGHJKLMNOPQRSTUVWXYZ", "abcdefghijkmnopqrstuvwxyz", "0123456789"};
            var rand = new Random();
            var chars = new List<char>();
            for(var i = 0; i <= randomChars.Length - 1; i ++){
                for(var j = 0; j < 2; j ++){
                    chars.Insert(rand.Next(0, chars.Count), randomChars[i][rand.Next(0, randomChars[i].Length)]);
                }
            }
            password = string.Join("", chars) + "@";
            return password;
        }
        private async Task ExcelExporProductSellerItemtClick(){
            var excelFileUrl = string.Empty;
            try{
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "Id";
                workSheet.Cells[1, 2].Value = "Ürün Adı";
                workSheet.Cells[1, 3].Value = "Stok";
                workSheet.Cells[1, 4].Value = "Fiyat";
                workSheet.Cells[1, 5].Value = "Ortalama Fiyat";
                workSheet.Cells[1, 6].Value = "Diğer İlanlar";
                workSheet.Cells[1, 7].Value = "Miat";
                workSheet.Cells[1, 8].Value = "Barkod";
                workSheet.Cells[1, 9].Value = "Kdv";
                workSheet.Cells[1, 10].Value = "Durumu";
                var recordIndex = 2;
                foreach(var item in _productSellerItems){
                    workSheet.Cells[recordIndex, 1].Value = item.Id;
                    workSheet.Cells[recordIndex, 2].Value = item.Product.Name;
                    workSheet.Cells[recordIndex, 3].Value = item.Stock;
                    workSheet.Cells[recordIndex, 4].Value = item.Price.ToString("n");
                    workSheet.Cells[recordIndex, 5].Value = item.AvgSameProductPrice.Value.ToString("n");
                    workSheet.Cells[recordIndex, 6].Value = item.CountSameProduct;
                    workSheet.Cells[recordIndex, 7].Value = item.ExprationDate.ToString("d");
                    workSheet.Cells[recordIndex, 8].Value = item.Product.Barcode;
                    workSheet.Cells[recordIndex, 9].Value = item.Product.Tax.TaxRate;
                    workSheet.Cells[recordIndex, 10].Value = item.Status == 1 ? "Aktif" : "Pasif";
                    recordIndex ++;
                }
                workSheet.Column(1).AutoFit();
                workSheet.Column(2).AutoFit();
                workSheet.Column(3).AutoFit();
                workSheet.Column(4).AutoFit();
                workSheet.Column(5).AutoFit();
                workSheet.Column(6).AutoFit();
                workSheet.Column(7).AutoFit();
                workSheet.Column(8).AutoFit();
                workSheet.Column(9).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream:new MemoryStream(fileBytes));
                await JSRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"Kullanici_Ilan_Listesi.xlsx", streamRef);
            } catch(Exception e){
                Console.WriteLine(e);
            }
        }
    }
}
