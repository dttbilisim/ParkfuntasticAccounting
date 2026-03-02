using System.Globalization;
using System.Net;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Sendeo.Exceptions;
using ecommerce.Cargo.Sendeo.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Sendeo.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class SendeoReturnDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public SendeoReturnDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        SendeoClient sendeoClient,
        ILogger logger)
    {
        _context = context;
        _sendeoClient = sendeoClient;
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
                     && o.Cargo.Name.ToLower().Contains("sendeo")
                     && o.ReturnShipmentDate > DateTime.UtcNow.AddDays(-14)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders.Chunk(50))
        {
            List<CargoListItem> shipmentStatuses;

            try
            {
                shipmentStatuses = (await _sendeoClient.GetCargoListAsync(
                    new CargoListRequest
                    {
                        ShipmentStartDate = orderChunks.Min(o => o.ReturnShipmentDate),
                        ShipmentEndDate = DateTime.UtcNow.AddHours(3),
                        TrackingNumbers = orderChunks.Select(o => long.Parse(o.ReturnCargoTrackNumber!)).ToList(),
                        PageNumber = 1,
                        PageCount = 50
                    }
                )).CargoList;
            }
            catch (SendeoApiException e)
            {
                if (e.HttpStatusCode == (int) HttpStatusCode.NotFound)
                {
                    continue;
                }

                _logger.LogError(e, "Error while getting shipment status from Sendeo API");
                continue;
            }

            foreach (var order in orderChunks)
            {
                try
                {
                    var sendeoShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.TrackingNumber == long.Parse(order.ReturnCargoTrackNumber!));

                    if (sendeoShipmentStatus == null)
                    {
                        continue;
                    }

                    if (sendeoShipmentStatus.StatusId != (int) SendeoCargoStatus.TeslimEdildi)
                    {
                        continue;
                    }

                    order.ReturnDeliveryDate = DateTime.TryParseExact(sendeoShipmentStatus.DeliveryDate, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deliveryDate)
                        ? deliveryDate
                        : DateTime.Now;
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