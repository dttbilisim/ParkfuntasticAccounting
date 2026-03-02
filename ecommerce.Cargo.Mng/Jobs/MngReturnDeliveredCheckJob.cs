using System.Net;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Exceptions;
using ecommerce.Cargo.Mng.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Mng.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class MngReturnDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public MngReturnDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        MngClient mngClient,
        ILogger logger)
    {
        _context = context;
        _mngClient = mngClient;
        _logger = logger;

        _orderRepository = _context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync()
    {
        var deliveryWaitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.ReturnCargoExternalId != null
                     && o.ReturnCargoTrackNumber != null
                     && o.ProblemStatus == OrderProblemStatus.WaitingCargoDelivery
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
                   
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders.Chunk(50))
        {
            List<GetShipmentStatusResponse> shipmentStatuses;

            try
            {
                shipmentStatuses = await _mngClient.GetShipmentStatusByShipmentId(orderChunks.Select(o => o.ReturnCargoTrackNumber!).ToList());
            }
            catch (MngApiException e)
            {
                if (e.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    continue;
                }

                _logger.LogError(e, "Error while getting shipment status from MNG API");
                continue;
            }

            foreach (var order in orderChunks)
            {
                try
                {
                    var mngShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.ShipmentId == order.ReturnCargoTrackNumber);

                    if (mngShipmentStatus == null)
                    {
                        continue;
                    }

                    if (!mngShipmentStatus.IsDelivered)
                    {
                        continue;
                    }

                    order.ReturnDeliveryDate = mngShipmentStatus.DeliveryDateTime;
                    order.ProblemStatus = OrderProblemStatus.WaitingReturnApproval;

                    updatedOrders.Add(order);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error while updating shipment delivery order id: {order.Id}, trackingNumber: {order.CargoTrackNumber}");
                }
            }
        }

        if (updatedOrders.Any())
        {
            _orderRepository.Update(updatedOrders);
        }

        await _context.SaveChangesAsync();
    }
}