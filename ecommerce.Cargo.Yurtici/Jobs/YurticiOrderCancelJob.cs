using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Cargo.Yurtici.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(YurticiOrderCancelJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class YurticiOrderCancelJob : IAsyncBackgroundJob<YurticiOrderCancelJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    public YurticiOrderCancelJob(IUnitOfWork<ApplicationDbContext> context, YurticiClient yurticiClient)
    {
        _context = context;
        _yurticiClient = yurticiClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(YurticiOrderCancelJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Seller)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.CargoExternalId != null
                     && o.CargoTrackNumber == null
                     && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew)
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

        var cancelResult = await _yurticiClient.CancelOrderAsync(
            new cancelNgiShipmentWithoutReturn
            {
                ngiDocumentKey = order.OrderNumber,
                cancellationDescription = order.OrderNumber
            }
        );
        var cancelShipment = cancelResult;

        if (cancelShipment?.outFlag != "0") //Başarılı değilse
        {
            throw new Exception("Could not cancel yurtici delivery: " + (cancelShipment?.outResult ?? cancelResult.errCode));
        }

        order.CargoExternalId = null;
        order.CargoRequestHandled = null;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}