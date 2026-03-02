using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Admin.Domain.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen;
namespace ecommerce.Admin.Components.Pages.Modals;
public partial class UpsertCompanyInterview{
    [Parameter] public int ? CompanyId{get;set;}
    [Parameter] public int ? Id{get;set;}
    [Inject] public ICompanyService CompanyService{get;set;}
    [Inject]
    protected DialogService DialogService { get; set; }
    [Inject] protected NotificationService NotificationService{get;set;}


    private CompanyInterviewDto _companyInterviewDto = new();
    protected override async Task OnInitializedAsync(){
       
        if(Id.Value!=0){
            var companyInterview = await CompanyService.GetCompanyInterview(CompanyId.Value);
            if(companyInterview.Ok){
                foreach(var interview in companyInterview.Result.Where(x=>x.Id==Id.Value)){
                    _companyInterviewDto.Id = interview.Id;
                    _companyInterviewDto.InterviewPersonel = interview.InterviewPersonel;
                    _companyInterviewDto.CompanyId = interview.CompanyId;
                    _companyInterviewDto.Message = interview.Message;
                    _companyInterviewDto.Created = interview.Created;
                    _companyInterviewDto.Updated = interview.Updated;
                }
            }
           
        } else{
            _companyInterviewDto.Created = DateTime.Now;
            _companyInterviewDto.CompanyId = CompanyId.Value;
            
        }
        StateHasChanged();
        
    }
   
    private async Task FormSubmit(){
        try{
           await CompanyService.UpsertCompanyInterview(_companyInterviewDto);
           DialogService.Close(null);
           NotificationService.Notify(NotificationSeverity.Success, "İşlem Tamamlandı");
            
        } catch(Exception e){
            Console.WriteLine(e);
            NotificationService.Notify(NotificationSeverity.Error, e.Message);
        }
    }
    protected void CancelButtonClick(MouseEventArgs args)
    {
        DialogService.Close(null);
    }
}
