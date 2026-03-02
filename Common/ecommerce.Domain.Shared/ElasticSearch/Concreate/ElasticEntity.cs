using ecommerce.Domain.Shared.ElasticSearch.Abstract;
using Nest;
namespace ecommerce.Domain.Shared.ElasticSearch.Concreate;
public class ElasticEntity<TEntityKey> : IElasticEntity<TEntityKey>{
    public virtual TEntityKey Id{get;set;}
    public virtual CompletionField Suggest{get;set;}
    public virtual string SearchingArea{get;set;}
    public virtual double ? Score{get;set;}
}