using System.Globalization;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Jobs;
using ecommerce.Cargo.Sendeo.Jobs;
using ecommerce.Cargo.Yurtici.Jobs;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class CargoReturnCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly IRepository<Orders> _orderRepository;

    private readonly IHangfireJobManager _hangfireJobManager;

    public CargoReturnCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        IHangfireJobManager hangfireJobManager)
    {
        _context = context;
        _hangfireJobManager = hangfireJobManager;

        _orderRepository = _context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync()
    {
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.ReturnCargoExternalId != null
                     && o.ProblemStatus == OrderProblemStatus.WaitingCargoShipment
                     && o.ReturnOrCancelDate != null && o.ReturnOrCancelDate.Value.Date <= DateTime.Now.Date.AddDays(-3)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var waitingOrder in waitingOrders)
        {
            waitingOrder.ProblemStatus = OrderProblemStatus.Cancelled;
            waitingOrder.ReturnOrCancelAdminDescription = "Kargo firmasına belirtilen sürede teslim edilmediği için iptal edildi.";

            updatedOrders.Add(waitingOrder);
        }

        if (!updatedOrders.Any())
        {
            return;
        }

        _orderRepository.Update(updatedOrders);

        await _context.SaveChangesAsync();

        foreach (var order in updatedOrders)
        {
            if (order.Cargo!.Name.ToLower().Contains("mng"))
            {
                await _hangfireJobManager.EnqueueAsync<MngReturnOrderCancelJob>(new MngReturnOrderCancelJobArgs { OrderId = order.Id });
            }
            else if (order.Cargo!.Name.ToLower().Contains("sendeo"))
            {
                await _hangfireJobManager.EnqueueAsync<SendeoReturnOrderCancelJob>(new SendeoReturnOrderCancelJobArgs { OrderId = order.Id });
            }
            // else if (order.Cargo!.Name.ToLower(new CultureInfo("tr-TR")).Contains("yurtiçi"))
            // {
            //     await _hangfireJobManager.EnqueueAsync<YurticiReturnOrderCancelJob>(new YurticiReturnOrderCancelJobArgs { OrderId = order.Id });
            // }
        }
    }
}