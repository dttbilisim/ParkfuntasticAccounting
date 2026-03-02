//using ecommerce.Admin2.EFCore.UnitOfWork;
//using ecommerce.Cargo.Yurtici.KOPSWebServices;
//using ecommerce.Core.BackgroundJobs;
//using ecommerce.Core.Entities;
//using ecommerce.Core.Utils;
//using ecommerce.EFCore.Context;
//using Microsoft.EntityFrameworkCore;

//namespace ecommerce.Cargo.Yurtici.Jobs;

//[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(YurticiReturnOrderCancelJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
//public class YurticiReturnOrderCancelJob : IAsyncBackgroundJob<YurticiReturnOrderCancelJobArgs>
//{
//    private readonly IUnitOfWork<ApplicationDbContext> _context;

//    private readonly YurticiClient _yurticiClient;

//    private readonly IRepository<Orders> _orderRepository;

//    public YurticiReturnOrderCancelJob(IUnitOfWork<ApplicationDbContext> context, YurticiClient yurticiClient)
//    {
//        _context = context;
//        _yurticiClient = yurticiClient;

//        _orderRepository = context.GetRepository<Orders>();
//    }

//    public async Task ExecuteAsync(YurticiReturnOrderCancelJobArgs args)
//    {
//        var order = await _orderRepository.GetAll(false)
//            .Include(o => o.Company)
//            .Include(o => o.Seller)
//            .Include(o => o.Cargo)
//            .Where(
//                o => o.Id == args.OrderId
//                     && o.ReturnCargoExternalId != null
//                     && o.ReturnCargoTrackNumber == null
//                     && o.ProblemStatus.HasValue
//                     && new[] { OrderProblemStatus.WaitingCargoShipment, OrderProblemStatus.Cancelled }.Contains(o.ProblemStatus.Value)
//                     && o.Cargo != null
//                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
//            )
//            .FirstOrDefaultAsync();

//        if (order == null)
//        {
//            return;
//        }

//        var yurticiResponse = await _yurticiClient.CancelReturnOrderAsync(
//            new cancelReturnShipmentCode
//            {
//                returnCode = order.ReturnCargoExternalId
//            }
//        );

//        if (yurticiResponse.errCode != "0")
//        {
//            throw new Exception("Could not cancel yurtici return delivery: " + yurticiResponse.errCode);
//        }

//        order.ReturnCargoExternalId = null;

//        _orderRepository.Update(order);

//        await _context.SaveChangesAsync();
//    }
//}