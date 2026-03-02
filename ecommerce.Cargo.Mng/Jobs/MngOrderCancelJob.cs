using System.Net;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Exceptions;
using ecommerce.Cargo.Mng.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Cargo.Mng.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(MngOrderCancelJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class MngOrderCancelJob : IAsyncBackgroundJob<MngOrderCancelJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    public MngOrderCancelJob(IUnitOfWork<ApplicationDbContext> context, MngClient mngClient)
    {
        _context = context;
        _mngClient = mngClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(MngOrderCancelJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Seller)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.CargoExternalId != null
                     && o.CargoTrackNumber == null
                     && o.OrderStatusType == OrderStatusType.OrderPrepare
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

        try
        {
            await _mngClient.CancelOrderAsync(new CancelCargoRequest{referenceId = order.OrderNumber,description = "Iptal edildi"});
        }
        catch (MngApiException e)
        {
            if (e.HttpStatusCode != (int) HttpStatusCode.NotFound)
            {
                throw;
            }
        }

        order.CargoExternalId = null;
        order.CargoRequestHandled = null;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}