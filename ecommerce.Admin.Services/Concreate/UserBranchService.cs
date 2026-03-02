using AutoMapper;
using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate
{
    public class UserBranchService : IUserBranchService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<UserBranch> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<UserBranchService> _logger;

        public UserBranchService(
            IUnitOfWork<ApplicationDbContext> context,
            IMapper mapper,
            ILogger<UserBranchService> logger)
        {
            _context = context;
            _repository = context.GetRepository<UserBranch>();
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult<List<UserBranchListDto>>> GetUserBranches(int userId)
        {
            var response = OperationResult.CreateResult<List<UserBranchListDto>>();

            try
            {
                var userBranches = await _repository.GetAll(disableTracking: true)
                    .Include(ub => ub.Branch)
                        .ThenInclude(b => b.Corporation)
                    .Where(ub => ub.UserId == userId && ub.Status != (int)EntityStatus.Deleted)
                    .ToListAsync();

                response.Result = userBranches.Select(ub => new UserBranchListDto
                {
                    Id = ub.Id,
                    UserId = ub.UserId,
                    BranchId = ub.BranchId,
                    BranchName = ub.Branch.Name,
                    CorporationId = ub.Branch.CorporationId,
                    CorporationName = ub.Branch.Corporation.Name,
                    IsDefault = ub.IsDefault
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserBranches Exception");
                response.AddSystemError(ex.Message);
            }

            return response;
        }

        public async Task<IActionResult<Empty>> UpsertUserBranches(int userId, List<UserBranchUpsertDto> branches)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                // Get existing user branches
                var existingBranches = await _repository.GetAll(disableTracking: true)
                    .Where(ub => ub.UserId == userId && ub.Status != (int)EntityStatus.Deleted)
                    .ToListAsync();

                // Delete removed branches
                var branchIdsToKeep = branches.Where(b => b.Id.HasValue).Select(b => b.Id.Value).ToList();
                var branchesToDelete = existingBranches.Where(eb => !branchIdsToKeep.Contains(eb.Id)).ToList();
                
                foreach (var branch in branchesToDelete)
                {
                    branch.Status = (int)EntityStatus.Deleted;
                    branch.DeletedDate = DateTime.Now;
                    _repository.Update(branch);
                }

                // Ensure only one default branch
                if (branches.Count(b => b.IsDefault) > 1)
                {
                    // Set only the first one as default
                    for (int i = 1; i < branches.Count; i++)
                    {
                        if (branches[i].IsDefault)
                            branches[i].IsDefault = false;
                    }
                }

                // Update or insert branches
                foreach (var branchDto in branches)
                {
                    if (branchDto.Id.HasValue && branchDto.Id.Value > 0)
                    {
                        // Update existing
                        var existing = existingBranches.FirstOrDefault(eb => eb.Id == branchDto.Id.Value);
                        if (existing != null)
                        {
                            existing.BranchId = branchDto.BranchId;
                            existing.IsDefault = branchDto.IsDefault;
                            existing.ModifiedDate = DateTime.Now;
                            _repository.Update(existing);
                        }
                    }
                    else
                    {
                        // Insert new
                        var newUserBranch = new UserBranch
                        {
                            UserId = userId,
                            BranchId = branchDto.BranchId,
                            IsDefault = branchDto.IsDefault,
                            Status = (int)EntityStatus.Active,
                            CreatedDate = DateTime.Now
                        };
                        await _repository.InsertAsync(newUserBranch);
                    }
                }

                await _context.SaveChangesAsync();
                response.AddSuccess("Şube atamaları başarıyla kaydedildi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertUserBranches Exception");
                response.AddSystemError(ex.Message);
            }

            return response;
        }

        public async Task<IActionResult<Empty>> DeleteUserBranch(int id)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {
                var userBranch = await _repository.GetFirstOrDefaultAsync(predicate: ub => ub.Id == id);
                
                if (userBranch == null)
                {
                    response.AddError("Şube ataması bulunamadı.");
                    return response;
                }

                userBranch.Status = (int)EntityStatus.Deleted;
                userBranch.DeletedDate = DateTime.Now;
                _repository.Update(userBranch);

                await _context.SaveChangesAsync();
                response.AddSuccess("Şube ataması silindi.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUserBranch Exception");
                response.AddSystemError(ex.Message);
            }

            return response;
        }
    }
}
