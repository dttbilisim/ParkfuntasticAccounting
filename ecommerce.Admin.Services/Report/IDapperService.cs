namespace ecommerce.Admin.Domain.Report;
public interface IDapperService{
    Task<List<T>> GetAll<T>(string query,object parms );
    Task<List<T>> Execute<T>(string functionName, object ? parameters = null);
    Task<bool> ExecuteRun(string functionName, object ? parameters = null);
    Task<bool> ExecuteRunner(string functionName, object ? parameters = null);
}
