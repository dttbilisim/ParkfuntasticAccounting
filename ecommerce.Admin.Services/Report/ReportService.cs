using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Report;
public class ReportService:IReportService{

    private readonly IDapperService _dbService;

    public ReportService(IDapperService dbService)
    {
        _dbService = dbService;
    }
    public async Task<List<T>> GetAll<T>(string query, object param=null){
        var rs = OperationResult.CreateResult<List<T>>();
        var response = await _dbService.GetAll<T>(query,param);
        rs.Result = response.ToList();
        return rs.Result;

    }
    public async Task<List<T>> Execute<T>(string functionName, object ? parameters = null){
        var rs = OperationResult.CreateResult<List<T>>();
        var response = await _dbService.Execute<T>(functionName,parameters);
        rs.Result = response;
        return rs.Result;
    }
    public async Task<bool> ExecuteRun(string functionName, object ? parameters = null){
        var rs = OperationResult.CreateResult(new bool());
        var response = await _dbService.ExecuteRun(functionName,parameters);
        rs.Result = response;
        return rs.Result;
    }
    public async Task<bool> ExecuteRunner(string functionName, object ? parameters = null){
        var rs = OperationResult.CreateResult(new bool());
        var response = await _dbService.ExecuteRunner(functionName,parameters);
        rs.Result = response;
        return rs.Result;
    }
}
