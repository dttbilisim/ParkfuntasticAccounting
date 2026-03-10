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
public class MngReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    private readonly IEmailService _emailService;

    public MngReadyCheckJob(
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
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Include(o => o.ApplicationUser)
            .Where(
                o =>  o.CargoTrackNumber == null
                     && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew)
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
                     && o.CreatedDate > DateTime.UtcNow.AddDays(-20)
                
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
        {
            List<GetShipmentStatusResponse> shipmentStatuses;

            try
            {
                shipmentStatuses = await _mngClient.GetShipmentStatus(orderChunks.Select(o => o.OrderNumber).ToList());
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
                    var mngShipmentStatus = shipmentStatuses.FirstOrDefault(s => s.ReferenceId == order.OrderNumber);

                    if (mngShipmentStatus == null)
                    {
                        continue;
                    }

                    order.CargoTrackNumber = mngShipmentStatus.ShipmentId;
                    order.CargoTrackUrl = mngShipmentStatus.TrackingUrl;
                    order.ShipmentDate = mngShipmentStatus.ShipmentDateTime;
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