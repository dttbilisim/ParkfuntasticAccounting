using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Cargo.Sendeo.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(SendeoOrderCancelJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class SendeoOrderCancelJob : IAsyncBackgroundJob<SendeoOrderCancelJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    public SendeoOrderCancelJob(IUnitOfWork<ApplicationDbContext> context, SendeoClient sendeoClient)
    {
        _context = context;
        _sendeoClient = sendeoClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(SendeoOrderCancelJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Seller)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.CargoExternalId != null
                     && o.CargoTrackNumber != null
                     && o.OrderStatusType == OrderStatusType.OrderPrepare
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("sendeo")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

        var cancelResult = await _sendeoClient.CancelDeliveryAsync(long.Parse(order.CargoTrackNumber!), order.OrderNumber);

        if (!cancelResult)
        {
            return;
        }

        order.CargoExternalId = null;
        order.CargoTrackNumber = null;
        order.CargoTrackUrl = null;
        order.CargoRequestHandled = null;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}