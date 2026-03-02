using System.Data;
using System.Text;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
namespace ecommerce.Admin.Domain.Report;
public class DapperService(IConfiguration configuration) : IDapperService{
    private readonly IDbConnection _db = new NpgsqlConnection(configuration.GetConnectionString("ApplicationDbContext"));
    public async Task<List<T>> GetAll<T>(string command, object parms){
        var result = (await _db.QueryAsync<T>(command, parms, commandType:CommandType.TableDirect)).ToList();
        return result;
    }
    public async Task<List<T>> Execute<T>(string functionName, object ? parameters = null){
        var command = new StringBuilder($"SELECT * FROM {functionName}(");
        if(parameters != null){
            var paramNames = new List<string>();
            foreach(var property in parameters.GetType().GetProperties()){
                paramNames.Add($"@{property.Name}");
            }
            command.Append(string.Join(",", paramNames));
        }
        command.Append(");");
        var result = await _db.QueryAsync<T>(command.ToString(), parameters);
        return result.ToList();
    }
    public async Task<bool> ExecuteRun(string functionName, object ? parameters = null){
        var command = new StringBuilder($"SELECT * FROM {functionName}(");
        if(parameters != null){
            var paramNames = parameters.GetType().GetProperties().Select(property => $"@{property.Name}").ToList();
            command.Append(string.Join(",", paramNames));
        }
        command.Append(");");
        await _db.QueryAsync(command.ToString(), parameters);
        return true;
    }
    public async Task<bool> ExecuteRunner(string functionName, object ? parameters = null){
        var command = new StringBuilder($"SELECT * FROM {functionName}(");
        if(parameters != null){
            var paramNames = parameters.GetType().GetProperties().Select(property => $"@{property.Name}").ToList();
            command.Append(string.Join(",", paramNames));
        }
        command.Append(");");
        await _db.ExecuteAsync(command.ToString(), parameters);
        return true;
    }
}
