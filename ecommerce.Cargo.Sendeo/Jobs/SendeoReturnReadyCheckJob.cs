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
public class SendeoReturnReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public SendeoReturnReadyCheckJob(
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
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.ReturnCargoExternalId != null
                     && o.ReturnCargoTrackNumber != null
                     && o.ProblemStatus == OrderProblemStatus.WaitingCargoShipment
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("sendeo")
                     && o.ReturnOrCancelDate > DateTime.UtcNow.AddDays(-7)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
        {
            List<CargoListItem> shipmentStatuses;

            try
            {
                shipmentStatuses = (await _sendeoClient.GetCargoListAsync(
                    new CargoListRequest
                    {
                        ShipmentStartDate = orderChunks.Min(o => o.ReturnOrCancelDate!.Value).AddHours(-3),
                        ShipmentEndDate = DateTime.UtcNow,
                        TrackingNumbers = orderChunks.Select(o => long.Parse(o.ReturnCargoTrackNumber!)).ToList(),
                        PageNumber = 0,
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

                    if (sendeoShipmentStatus.StatusId == (int) SendeoCargoStatus.KargoSevkEmriAlindi)
                    {
                        continue;
                    }

                    order.ReturnShipmentDate = sendeoShipmentStatus.ShipmentDateTime;
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