using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IProductMapper<T>
{
    Product Map(T item);
}
