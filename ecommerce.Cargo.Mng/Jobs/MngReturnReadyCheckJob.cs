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
public class MngReturnReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public MngReturnReadyCheckJob(
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
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.ReturnCargoExternalId != null
                     && o.ReturnCargoReference != null
                     && o.ReturnCargoTrackNumber == null
                     && o.ProblemStatus == OrderProblemStatus.WaitingCargoShipment
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
                     && o.ReturnOrCancelDate > DateTime.UtcNow.AddDays(-7)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
        {
            List<GetShipmentStatusResponse> shipmentStatuses;

            try
            {
                shipmentStatuses = await _mngClient.GetShipmentStatus(orderChunks.Select(o => o.ReturnCargoReference!).ToList());
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
                    var mngShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.ReferenceId == order.ReturnCargoReference);

                    if (mngShipmentStatus == null)
                    {
                        continue;
                    }

                    order.ReturnCargoTrackNumber = mngShipmentStatus.ShipmentId;
                    order.ReturnCargoTrackUrl = mngShipmentStatus.TrackingUrl;
                    order.ReturnShipmentDate = mngShipmentStatus.ShipmentDateTime;
                    order.ProblemStatus = OrderProblemStatus.WaitingCargoDelivery;

                    updatedOrders.Add(order);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error while updating shipment tracking number order id: {order.Id}");
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