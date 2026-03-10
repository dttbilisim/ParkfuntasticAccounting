using System.Net;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Sendeo.Exceptions;
using ecommerce.Cargo.Sendeo.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Sendeo.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class SendeoReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    private readonly IEmailService _emailService;

    public SendeoReadyCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        SendeoClient sendeoClient,
        ILogger logger,
        IEmailService emailService)
    {
        _context = context;
        _sendeoClient = sendeoClient;
        _logger = logger;
        _emailService = emailService;

        _orderRepository = _context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync()
    {
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Include(o => o.ApplicationUser).Include(x=>x.Seller)
            .Where(
                o => o.CargoTrackUrl != null
                     && o.CargoTrackNumber != null
                     && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew)
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("sendeo")
                     
                    
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
        {
            List<CargoListItem> shipmentStatuses;

            try
            {
                var request = new CargoListRequest
                {
                    ShipmentStartDate = orderChunks.Min(o => o.CreatedDate).AddHours(-3),
                    ShipmentEndDate = DateTime.UtcNow,
                    TrackingNumbers = orderChunks.Select(o => long.Parse(o.CargoTrackNumber!)).ToList(),
                    PageNumber = 0,
                    PageCount = 50
                };
                
                 var response = await _sendeoClient.GetCargoListAsync(request);
                 _logger.LogInformation($"Sendeo CargoList Response: {Newtonsoft.Json.JsonConvert.SerializeObject(response)}");

                 shipmentStatuses = response.CargoList;
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
                    var sendeoShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.TrackingNumber == long.Parse(order.CargoTrackNumber!));

                    if (sendeoShipmentStatus == null)
                    {
                        continue;
                    }

                    if (sendeoShipmentStatus.StatusId == (int) SendeoCargoStatus.KargoSevkEmriAlindi)
                    {
                        continue;
                    }

                    order.ShipmentDate = sendeoShipmentStatus.ShipmentDateTime;
                    order.OrderStatusType = OrderStatusType.OrderinCargo;
                    order.CargoRequestHandled = true;

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

        foreach (var order in updatedOrders)
        {
            if (order.OrderStatusType != OrderStatusType.OrderinCargo)
            {
                continue;
            }

            await _emailService.SendOrderShippedEmail(order);
        }
    }
}