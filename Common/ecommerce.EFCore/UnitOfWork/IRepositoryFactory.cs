using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EFCore.UnitOfWork;
namespace ecommerce.Admin.EFCore;

public interface IRepositoryFactory
{
    IRepository<TEntity> GetRepository<TEntity>(bool hasCustomRepository = false) where TEntity : class;
}