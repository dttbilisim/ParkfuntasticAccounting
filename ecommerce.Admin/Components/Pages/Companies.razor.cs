using ecommerce.Admin.Components.Pages.Modals;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Dtos.MembershipDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services;
using ecommerce.Core.Extensions;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Radzen;
using Radzen.Blazor;

namespace ecommerce.Admin.Components.Pages{
    public partial class Companies{
        #region Injections
        [Inject] protected NavigationManager NavigationManager{get;set;}
        [Inject] protected DialogService DialogService{get;set;}
        [Inject] public ICompanyService CompanyService{get;set;}
        [Inject] protected AuthenticationService Security{get;set;}
        [Inject] protected NotificationService NotificationService{get;set;}
        [Inject] public ICityService CityService{get;set;}
        [Inject] public ITownService TownService{get;set;}
        [Inject] protected IJSRuntime _JsRuntime{get;set;}
        #endregion
        int count;
        protected List<CityListDto> cities = new();
        protected List<TownListDto> towns = new();
        protected List<CompanyListDto> companies = null;
        protected RadzenDataGrid<CompanyListDto> ? radzenDataGrid = new();
        protected RadzenDataFilter<CompanyListDto> ? dataFilter;
        
        private new DialogOptions DialogOptions = new(){Width = "1200px"};
        private PageSetting pager;
        //protected override async Task OnAfterRenderAsync(bool firstRender){
        //    var cityResponse = CityService.GetCities();
        //    if(cityResponse.Result.Ok) cities = cityResponse.Result.Result;
        //    if(firstRender){
        //        await dataFilter.AddFilter(new CompositeFilterDescriptor(){Property = "Status", FilterValue = EntityStatus.Active.GetHashCode(), FilterOperator = Radzen.FilterOperator.Equals});
        //    }
        //}
        protected async Task AddButtonClick(MouseEventArgs args){
            await DialogService.OpenAsync<UpsertCompany>("Kullanıcı Ekle", null, DialogOptions);
            await radzenDataGrid.Reload();
        }
        protected async Task EditRow(CompanyListDto args){
            await DialogService.OpenAsync<UpsertCompany>("Kullanıcı Düzenle", new Dictionary<string, object>{{"Id", args.Id}}, DialogOptions);
            await radzenDataGrid.Reload();
        }
        
        protected async Task AddRow(CompanyListDto args){
            await DialogService.OpenAsync<UpsertCompany>("Kullanıcı Ekle", new Dictionary<string, object>{{"Id", null}}, DialogOptions);
            await radzenDataGrid.Reload();
        }
        protected async Task GridDeleteButtonClick(MouseEventArgs args, CompanyListDto company){
            try{
                if(await DialogService.Confirm("Seçilen kullanıcı silinecek ve aynı zamanda ilanları pasife alınacak. Sistemde giriş yapamayacaktır. Onaylıyor musunuz?", "Kayıt Sil", new ConfirmOptions(){OkButtonText = "Evet", CancelButtonText = "Hayır"}) == true){
                    var deleteResult = await CompanyService.DeleteCompany(new Core.Helpers.AuditWrapDto<CompanyDeleteDto>(){UserId = Security.User.Id, Dto = new CompanyDeleteDto(){Id = company.Id}});
                    if(deleteResult != null){
        
                        await radzenDataGrid.Reload();
                    }
                }
            } catch(Exception ex){
                NotificationService.Notify(new NotificationMessage{Severity = NotificationSeverity.Error, Summary = $"Error", Detail = $"Unable to delete product"});
            }
        }
        private async Task LoadData(LoadDataArgs args){

            var orderfilter = args.OrderBy.Replace("np", "") == "" ? "Id desc" : args.OrderBy.Replace("np", "");
            args.Filter = args.Filter.Replace("np", "");
            pager = new PageSetting(args.Filter, orderfilter, args.Skip, args.Top);
            var companyResponse = await CompanyService.GetCompanies(pager);
            if(companyResponse.Ok && companyResponse.Result != null && companyResponse.Result.Data != null){
                companies = companyResponse.Result.Data.OrderByDescending(x=>x.Id).ToList();
                count = companyResponse.Result.DataCount;
            } else
                if(companyResponse.Exception != null){
                    NotificationService.Notify(NotificationSeverity.Error, companyResponse.GetMetadataMessages());
                }
            StateHasChanged();
        }
        protected async Task LoadCityOfTowns(object value){
            var townResponse = await TownService.GetTownsByCityId((int) value);
            if(townResponse.Ok) towns = townResponse.Result;
            StateHasChanged();
        }
        void RowRender(RowRenderEventArgs<CompanyListDto> args){
            if(args.Data.Status == EntityStatus.Passive)
                args.Attributes.Add("style", $"background-color: #FFEFEF;");
            else
                if(args.Data.Status == EntityStatus.Deleted) args.Attributes.Add("style", $"background-color: #FFE1E1;");
        }
         public async Task ExcelExportClick()
        {
            var excelFileUrl = string.Empty;
   

            pager = new PageSetting(null,null, 0,int.MaxValue,true);
            var response = await CompanyService.GetCompanies(pager);

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Sheet1");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "İl";
                workSheet.Cells[1, 2].Value = "İlçe";
                workSheet.Cells[1, 3].Value = "Çalışma Tipi";
                workSheet.Cells[1, 4].Value = "Kullanıcı Tipi";
                workSheet.Cells[1, 5].Value = "Adı";
                workSheet.Cells[1, 6].Value = "Soyadı";
                workSheet.Cells[1, 7].Value = "Email";
                workSheet.Cells[1, 8].Value = "Gln Numarası";
                workSheet.Cells[1, 9].Value = "Durum";

                
                var recordIndex = 2;
                foreach (var item in response.Result.Data.ToList())
                {
                    workSheet.Cells[recordIndex, 1].Value = item.City.Name;
                    workSheet.Cells[recordIndex, 2].Value = item.Town.Name;
                    workSheet.Cells[recordIndex, 3].Value = item.CompanyWorkingType.GetDisplayName();
                    workSheet.Cells[recordIndex, 4].Value = item.UserType.GetDisplayName();
                    workSheet.Cells[recordIndex, 5].Value = item.FirstName;
                    workSheet.Cells[recordIndex, 6].Value = item.LastName;
                    workSheet.Cells[recordIndex, 7].Value = item.EmailAddress;
                    workSheet.Cells[recordIndex, 8].Value = item.GlnNumber;
                    workSheet.Cells[recordIndex, 9].Value = item.Status.GetDisplayName();
                    recordIndex++;
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
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
                await _JsRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"Kullanici.xlsx", streamRef);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }

        }
        public async Task CrmExportClick(){
              var excelFileUrl = string.Empty;
   

            pager = new PageSetting(null,null, 0,int.MaxValue,true);
            var response = await CompanyService.GetCompanies(pager);

            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                var excel = new ExcelPackage();
                var workSheet = excel.Workbook.Worksheets.Add("Data");
                workSheet.TabColor = System.Drawing.Color.Black;
                workSheet.DefaultRowHeight = 12;
                workSheet.Row(1).Height = 20;
                workSheet.Row(1).Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                workSheet.Row(1).Style.Font.Bold = true;
                workSheet.Cells[1, 1].Value = "CompanyName";
                workSheet.Cells[1, 2].Value = "Name";
                workSheet.Cells[1, 3].Value = "Surname";
                workSheet.Cells[1, 4].Value = "Phone";
                workSheet.Cells[1, 5].Value = "Email";
                workSheet.Cells[1, 6].Value = "Address";
                workSheet.Cells[1, 7].Value = "Country";
                workSheet.Cells[1, 8].Value = "City";
                workSheet.Cells[1, 9].Value = "TaxOffice";
                workSheet.Cells[1, 10].Value = "TaxNumber";
                workSheet.Cells[1, 11].Value = "IDNumber";
                workSheet.Cells[1, 12].Value = "Sector";
                workSheet.Cells[1, 13].Value = "Tag";
                workSheet.Cells[1, 14].Value = "Notes";

                
                var recordIndex = 2;
                foreach (var item in response.Result.Data.ToList()){
                    workSheet.Cells[recordIndex, 1].Value = item.BankAccountName ?? item.AccountName;
                    workSheet.Cells[recordIndex, 2].Value = item.FirstName;
                    workSheet.Cells[recordIndex, 3].Value = item.LastName;
                    workSheet.Cells[recordIndex, 4].Value = item.PhoneNumber ?? item.PhoneNumberSecond;
                    workSheet.Cells[recordIndex, 5].Value = item.EmailAddress;
                    workSheet.Cells[recordIndex, 6].Value = item.Address;
                    workSheet.Cells[recordIndex, 7].Value = "90";
                    workSheet.Cells[recordIndex, 8].Value = item.CityId;
                    workSheet.Cells[recordIndex, 9].Value = item.TaxName;
                    workSheet.Cells[recordIndex, 10].Value = item.TaxNumber;
                    workSheet.Cells[recordIndex, 11].Value = item.GlnNumber;
           
                    workSheet.Cells[recordIndex, 12].Value = "";
                    workSheet.Cells[recordIndex, 13].Value = "";
                    workSheet.Cells[recordIndex, 14].Value = "";
                    recordIndex++;
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
                workSheet.Column(10).AutoFit();
                workSheet.Column(11).AutoFit();
                workSheet.Column(12).AutoFit();
                workSheet.Column(13).AutoFit();
                 workSheet.Column(14).AutoFit();
                var fileBytes = await excel.GetAsByteArrayAsync();
                using var streamRef = new DotNetStreamReference(stream: new MemoryStream(fileBytes));
                await _JsRuntime.InvokeVoidAsync("ecommerce.downloadFileFromStream", $"CrmUserList.xlsx", streamRef);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);

            }
        }
    }
}
