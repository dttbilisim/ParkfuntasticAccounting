namespace ecommerce.Domain.Shared.ElasticSearch.Abstract;
public interface IElasticEntity<TEntityKey>{
    TEntityKey Id{get;set;}
}
