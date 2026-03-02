using AutoMapper;
using ecommerce.Admin.Domain.Dtos.FrequentlyAskedQuestionsDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace ecommerce.Admin.Domain.Concreate
{
    public class FrequentlyAskedQuestionService : IFrequentlyAskedQuestionService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly IRepository<FrequentlyAskedQuestion> _repository;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;
        private readonly IRadzenPagerService<FrequentlyAskedQuestionListDto> _radzenPagerService;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private const string MENU_NAME = "sssandblog";

        public FrequentlyAskedQuestionService(
            IUnitOfWork<ApplicationDbContext> context,
            IMapper mapper,
            ILogger logger,
            IRadzenPagerService<FrequentlyAskedQuestionListDto> radzenPagerService,
            ecommerce.Admin.Domain.Services.IPermissionService permissionService)
        {
            _context = context;
            _repository = context.GetRepository<FrequentlyAskedQuestion>();
            _mapper = mapper;
            _logger = logger;
            _radzenPagerService = radzenPagerService;
            _permissionService = permissionService;
        }

        public async Task<IActionResult<Empty>> DeleteFrequentlyAskedQuestion(AuditWrapDto<FrequentlyAskedQuestionDeleteDto> model)
        {
            var response = new IActionResult<Empty> { Result = new Empty() };

            try
            {

                var sss = _context.DbContext.FrequentlyAskedQuestions.FirstOrDefault(f => f.Id == model.Dto.Id);
                sss.Status = (int)EntityStatus.Deleted;
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    response.AddSuccess("Successfull");
                    return response;
                }

                if (lastResult != null && lastResult.Exception != null)
                    response.AddError(lastResult.Exception.ToString());

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"DeleteFrequentlyAskedQuestion Exception: {ex.ToString()}");
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Paging<IQueryable<FrequentlyAskedQuestionListDto>>>> GetFrequentlyAskedQuestions(PageSetting pager)
        {
            IActionResult<Paging<IQueryable<FrequentlyAskedQuestionListDto>>> response = new() { Result = new() };
            try
            {

                var faqList = await _repository.GetAllAsync(predicate: f => f.Id != 0 && f.Status == (int)EntityStatus.Active);
                var mappedCats = _mapper.Map<List<FrequentlyAskedQuestionListDto>>(faqList);
                if (mappedCats != null)
                {
                    if (mappedCats.Count > 0)
                    {
                        response.Result.Data = mappedCats.AsQueryable();
                        foreach (var item in response.Result.Data)
                        {
                            if (item.ParentId.HasValue)
                                item.ParentName = response.Result.Data.FirstOrDefault(x => x.Id == item.ParentId)?.Name;
                        }
                        response.Result.DataCount= mappedCats.Count;
                    }
                }


                if (response.Result.Data != null)
                    response.Result.Data = response.Result.Data.OrderByDescending(x => x.Id);

                var result = _radzenPagerService.MakeDataQueryable(response.Result.Data, pager);


                response.Result = result;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFrequentlyAskedQuestion Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<FrequentlyAskedQuestionUpsertDto>> GetFrequentlyAskedQuestionById(int Id)
        {
            var rs = new IActionResult<FrequentlyAskedQuestionUpsertDto>
            {
                Result = new FrequentlyAskedQuestionUpsertDto()
            };
            try
            {
                var sss = await _repository.GetFirstOrDefaultAsync(predicate: f => f.Id == Id);
                var mapped = _mapper.Map<FrequentlyAskedQuestionUpsertDto>(sss);
                if (mapped != null)
                {
                    rs.Result = mapped;
                }
                else rs.AddError("SSS Bulunamadı");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFrequentlyAskedQuestionById Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }

        public async Task<IActionResult<List<FrequentlyAskedQuestionListDto>>> GetFrequentlyAskedQuestions()
        {
            IActionResult<List<FrequentlyAskedQuestionListDto>> response = new() { Result = new() };
            try
            {

                var sssList = _repository.GetAll(predicate: f => f.Status == (int)EntityStatus.Active);
                var mapped = _mapper.Map<List<FrequentlyAskedQuestionListDto>>(sssList);
                if (mapped != null)
                {
                    if (mapped.Count > 0)
                        response.Result = mapped.ToList();
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFrequentlyAskedQuestions Exception " + ex.ToString());
                response.AddSystemError(ex.ToString());
                return response;
            }
        }

        public async Task<IActionResult<Empty>> UpsertFrequentlyAskedQuestion(AuditWrapDto<FrequentlyAskedQuestionUpsertDto> model)
        {
            var rs = new IActionResult<Empty>
            {
                Result = new Empty()
            };
            try
            {
                var dto = model.Dto;
                var entity = _mapper.Map<FrequentlyAskedQuestion>(dto);
                if (!dto.Id.HasValue)
                {
                    entity.Status =(int)EntityStatus.Active;
                    entity.CreatedId = model.UserId;
                    entity.CreatedDate = DateTime.Now;
                    await _repository.InsertAsync(entity);
                }
                else
                {
                    entity = await _context.DbContext.FrequentlyAskedQuestions.FirstOrDefaultAsync(x => x.Id == dto.Id);

                    entity.Name = dto.Name;
                    entity.Group = dto.Group;
                    entity.ParentId = dto.ParentId;
                    entity.Description = dto.Description;
                    entity.Order = dto.Order;

                    entity.Status = (int)EntityStatus.Active;
                    entity.ModifiedId = model.UserId;
                    entity.ModifiedDate = DateTime.Now;
                    _repository.Update(entity);
                }
                await _context.SaveChangesAsync();
                var lastResult = _context.LastSaveChangesResult;
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Successfull");
                    return rs;
                }
                else
                {
                    if (lastResult != null && lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("UpsertFrequentlyAskedQuestion Exception " + ex.ToString());
                rs.AddSystemError(ex.ToString());
                return rs;
            }
        }
    }
}
