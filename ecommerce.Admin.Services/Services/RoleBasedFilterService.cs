using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ecommerce.Admin.Domain.Services
{
    /// <summary>
    /// Centralized service for role-based data filtering
    /// Provides generic filtering methods for all entities with BranchId
    /// </summary>
    public interface IRoleBasedFilterService
    {
        /// <summary>
        /// Apply role-based filtering to any queryable entity with BranchId
        /// </summary>
        IQueryable<T> ApplyFilter<T>(IQueryable<T> query, ApplicationDbContext dbContext) where T : class;

        /// <summary>
        /// Checks if the current user has access to the specified branch
        /// </summary>
        Task<bool> CanAccessBranchAsync(int branchId, ApplicationDbContext dbContext);

        /// <summary>
        /// Kullanıcının erişebildiği branch ID listesini döndürür (UserBranches tablosundan)
        /// </summary>
        Task<List<int>> GetAllowedBranchIdsAsync(ApplicationDbContext dbContext);
    }

    public class RoleBasedFilterService : IRoleBasedFilterService
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public RoleBasedFilterService(ITenantProvider tenantProvider, IHttpContextAccessor httpContextAccessor)
        {
            _tenantProvider = tenantProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> CanAccessBranchAsync(int branchId, ApplicationDbContext dbContext)
        {
            // ... (keep existing implementation)
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            if (isGlobalAdmin) return true;

            var currentBranchId = _tenantProvider.GetCurrentBranchId();
            if (currentBranchId > 0 && branchId == currentBranchId) return true;

            // Branch 0 is Global, everyone should be able to access global records
            if (branchId == 0) return true;

            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId)) return false;

            var isAllowed = await dbContext.UserBranches
                .AsNoTracking()
                .AnyAsync(ub => ub.UserId == userId && ub.BranchId == branchId && ub.Status == (int)EntityStatus.Active);
            
            return isAllowed;
        }

        /// <summary>
        /// Kullanıcının erişebildiği branch ID listesini UserBranches tablosundan döndürür.
        /// Kullanıcı kimliği alınamazsa boş liste döner.
        /// </summary>
        public async Task<List<int>> GetAllowedBranchIdsAsync(ApplicationDbContext dbContext)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
                return new List<int>();

            return await dbContext.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                .Select(ub => ub.BranchId)
                .ToListAsync();
        }

        public IQueryable<T> ApplyFilter<T>(IQueryable<T> query, ApplicationDbContext dbContext) where T : class
        {
            var isGlobalAdmin = _tenantProvider.IsGlobalAdmin;
            var isB2BAdmin = _tenantProvider.IsB2BAdmin;
            var currentBranchId = _tenantProvider.GetCurrentBranchId();

            var entityType = typeof(T);
            var branchIdProperty = entityType.GetProperty("BranchId");
            
            if (branchIdProperty == null) return query;

            var isBranchIdNullable = Nullable.GetUnderlyingType(branchIdProperty.PropertyType) != null 
                                    || !branchIdProperty.PropertyType.IsValueType;

            if (isGlobalAdmin)
            {
                return ApplyAdminFilter(query, currentBranchId, isBranchIdNullable);
            }
            else if (isB2BAdmin)
            {
                return ApplyB2BAdminFilter(query, dbContext, currentBranchId, isBranchIdNullable);
            }
            else if (_tenantProvider.IsPlasiyer || _tenantProvider.IsCustomerB2B)
            {
                // SPECIAL CASE: B2B Customers should see their global balance regardless of selected branch
                if (_tenantProvider.IsCustomerB2B && typeof(T).Name == "CustomerAccountTransaction")
                {
                    return ApplyAdminFilter(query, 0, isBranchIdNullable);
                }

                // SPECIAL CASE: B2B Customers must be able to see their own Customer record
                // even if it belongs to a different branch (e.g. branch 0 or main branch)
                if (_tenantProvider.IsCustomerB2B && typeof(T).Name == "Customer")
                {
                    return query; // No branch filtering for Customer entity
                }

                // Applying BranchId filter even for these roles ensures they only see what belongs to the branch
                return ApplyAdminFilter(query, currentBranchId, isBranchIdNullable);
            }

            return query.Where(x => false);
        }

        private IQueryable<T> ApplyAdminFilter<T>(IQueryable<T> query, int currentBranchId, bool isNullable) where T : class
        {
             // ... (keep existing implementation)
             if (currentBranchId == 0) return query;
             
             // ... logic same as before ... 
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            var property = System.Linq.Expressions.Expression.Property(parameter, "BranchId");
            System.Linq.Expressions.Expression comparison;
            if (isNullable)
            {
                var hasValue = System.Linq.Expressions.Expression.Property(property, "HasValue");
                var notHasValue = System.Linq.Expressions.Expression.Not(hasValue);
                var valueProperty = System.Linq.Expressions.Expression.Property(property, "Value");
                // Check for 0
                var zero = System.Linq.Expressions.Expression.Constant(0);
                var isZero = System.Linq.Expressions.Expression.Equal(valueProperty, zero);
                var isZeroIfHasValue = System.Linq.Expressions.Expression.AndAlso(hasValue, isZero);

                var constant = System.Linq.Expressions.Expression.Constant(currentBranchId);
                var equal = System.Linq.Expressions.Expression.Equal(valueProperty, constant);
                
                var globalOrEqual = System.Linq.Expressions.Expression.OrElse(equal, isZeroIfHasValue);
                comparison = System.Linq.Expressions.Expression.OrElse(notHasValue, globalOrEqual);
            }
            else
            {
                var constant = System.Linq.Expressions.Expression.Constant(currentBranchId);
                var equal = System.Linq.Expressions.Expression.Equal(property, constant);

                // Allow 0 for non-nullable
                var zero = System.Linq.Expressions.Expression.Constant(0);
                var isZero = System.Linq.Expressions.Expression.Equal(property, zero);
                
                comparison = System.Linq.Expressions.Expression.OrElse(equal, isZero);
            }
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, parameter);
            return query.Where(lambda);
        }

        private IQueryable<T> ApplyB2BAdminFilter<T>(IQueryable<T> query, ApplicationDbContext dbContext, int currentBranchId, bool isNullable) where T : class
        {
            // ... logic ...
            var user = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return query.Where(x => false);
            } 
            var allowedBranchIds = dbContext.UserBranches
                .AsNoTracking()
                .Where(ub => ub.UserId == userId && ub.Status == (int)EntityStatus.Active)
                .Select(ub => ub.BranchId)
                .ToList();

            if (!allowedBranchIds.Any())
            {
                 if (isNullable)
                {
                    // Allow global records
                    // Allow global records (NULL or 0)
                    var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "BranchId");
                    var hasValue = System.Linq.Expressions.Expression.Property(property, "HasValue");
                    var notHasValue = System.Linq.Expressions.Expression.Not(hasValue);
                    
                    // Check for 0 value
                    var valueProperty = System.Linq.Expressions.Expression.Property(property, "Value");
                    var zero = System.Linq.Expressions.Expression.Constant(0);
                    var isZero = System.Linq.Expressions.Expression.Equal(valueProperty, zero);
                    var isZeroIfHasValue = System.Linq.Expressions.Expression.AndAlso(hasValue, isZero);

                    var isGlobal = System.Linq.Expressions.Expression.OrElse(notHasValue, isZeroIfHasValue);

                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(isGlobal, parameter);
                    return query.Where(lambda);
                }
                return query.Where(x => false);
            }

            if (currentBranchId > 0)
            {
                if (allowedBranchIds.Contains(currentBranchId))
                {
                     var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
                    var property = System.Linq.Expressions.Expression.Property(parameter, "BranchId");
                    System.Linq.Expressions.Expression comparison;
                    if (isNullable)
                    {
                        var hasValue = System.Linq.Expressions.Expression.Property(property, "HasValue");
                        var notHasValue = System.Linq.Expressions.Expression.Not(hasValue);
                        var valueProperty = System.Linq.Expressions.Expression.Property(property, "Value");
                        
                        // Check for 0
                        var zero = System.Linq.Expressions.Expression.Constant(0);
                        var isZero = System.Linq.Expressions.Expression.Equal(valueProperty, zero);
                        var isZeroIfHasValue = System.Linq.Expressions.Expression.AndAlso(hasValue, isZero);
                        
                        var constant = System.Linq.Expressions.Expression.Constant(currentBranchId);
                        var equal = System.Linq.Expressions.Expression.Equal(valueProperty, constant);
                        
                        var allowed = System.Linq.Expressions.Expression.OrElse(equal, isZeroIfHasValue);
                        comparison = System.Linq.Expressions.Expression.OrElse(notHasValue, allowed);
                    }
                    else
                    {
                        var constant = System.Linq.Expressions.Expression.Constant(currentBranchId);
                        var equal = System.Linq.Expressions.Expression.Equal(property, constant);

                         // Allow 0 for non-nullable
                        var zero = System.Linq.Expressions.Expression.Constant(0);
                        var isZero = System.Linq.Expressions.Expression.Equal(property, zero);
                
                        comparison = System.Linq.Expressions.Expression.OrElse(equal, isZero);
                    }
                    var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(comparison, parameter);
                    return query.Where(lambda);
                }
                else
                {
                    return query.Where(x => false);
                }
            }

            // Merkez seçili (currentBranchId == 0): sadece BranchId null veya 0
            var param = System.Linq.Expressions.Expression.Parameter(typeof(T), "x");
            var prop = System.Linq.Expressions.Expression.Property(param, "BranchId");
            System.Linq.Expressions.Expression globalOnly;
            if (isNullable)
            {
                var hasValue = System.Linq.Expressions.Expression.Property(prop, "HasValue");
                var notHasValue = System.Linq.Expressions.Expression.Not(hasValue);
                var valueProp = System.Linq.Expressions.Expression.Property(prop, "Value");
                var zero = System.Linq.Expressions.Expression.Constant(0);
                var isZero = System.Linq.Expressions.Expression.Equal(valueProp, zero);
                var isZeroIfHasValue = System.Linq.Expressions.Expression.AndAlso(hasValue, isZero);
                globalOnly = System.Linq.Expressions.Expression.OrElse(notHasValue, isZeroIfHasValue);
            }
            else
            {
                var zero = System.Linq.Expressions.Expression.Constant(0);
                globalOnly = System.Linq.Expressions.Expression.Equal(prop, zero);
            }
            var lambdaExpr = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(globalOnly, param);
            return query.Where(lambdaExpr);
        }
    }
}
