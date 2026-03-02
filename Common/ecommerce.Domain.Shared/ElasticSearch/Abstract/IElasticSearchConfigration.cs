namespace ecommerce.Domain.Shared.ElasticSearch.Abstract;
public interface IElasticSearchConfigration{
    string ConnectionString{get;}
    string AuthUserName{get;}
    string AuthPassWord{get;}
}