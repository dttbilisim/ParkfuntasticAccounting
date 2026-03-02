using System.Net;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Exceptions;
using ecommerce.Cargo.Mng.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Mng.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class MngDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;
    private readonly IEmailService _emailService;

    private readonly ILogger _logger;

    public MngDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        MngClient mngClient,
        ILogger logger,
            IEmailService emailService)
    {
        _context = context;
        _mngClient = mngClient;
        _logger = logger;
        _emailService = emailService;

        _orderRepository = _context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync()
    {
        var deliveryWaitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o =>  o.CargoTrackNumber != null
                     && o.OrderStatusType == OrderStatusType.OrderinCargo
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
                     && o.DeliveryDate==null
                  
                   
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders.Chunk(50))
        {
            List<GetShipmentStatusResponse> shipmentStatuses;

            try
            {
                shipmentStatuses = await _mngClient.GetShipmentStatusByShipmentId(orderChunks.Select(o => o.CargoTrackNumber!).ToList());
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
                    var mngShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.ShipmentId == order.CargoTrackNumber);

                    if (mngShipmentStatus == null)
                    {
                        continue;
                    }

                    if (!mngShipmentStatus.IsDelivered)
                    {
                        continue;
                    }

                    order.DeliveryDate = mngShipmentStatus.DeliveryDateTime;
                    order.DeliveryTo = mngShipmentStatus.DeliveryTo;
                    order.OrderStatusType = OrderStatusType.OrderSuccess;
                    order.IsSellerApproved = true;

                    updatedOrders.Add(order);
                    await _emailService.SendOrderShippedEmail(order);
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
