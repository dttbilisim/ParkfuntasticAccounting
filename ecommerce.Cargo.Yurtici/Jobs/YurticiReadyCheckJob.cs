using System.Globalization;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Yurtici.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class YurticiReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    private readonly IEmailService _emailService;

    public YurticiReadyCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        YurticiClient yurticiClient,
        ILogger logger,
        IEmailService emailService)
    {
        _context = context;
        _yurticiClient = yurticiClient;
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
                o => o.CargoTrackUrl == null
                     && o.CargoTrackNumber == null
                     && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew)
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
                     && o.CreatedDate > DateTime.UtcNow.AddDays(-20)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
        {
            var shipmentResult = await _yurticiClient.GetShipmentStatusAsync(
                 new KOPSOrderStatusWebServices.listInvDocumentInterfaceByReference()
                 {
                     //  docIdArray = orderChunks.Select(o => o.Id.ToString()).ToArray()

                     withCargoLifecycle = "1",
                     fieldName = "3",
                     fieldValueArray = orderChunks.Select(o => o.OrderNumber.ToString()).ToArray(),


                 }
            ) ;
            var shipmentStatuses = shipmentResult.ShippingDataResponseVO;

            foreach (var order in orderChunks)
            {
                try
                {
                    var yurticiShipmentStatus = shipmentStatuses.shippingDataDetailVOArray.Where(f => f.transactionStatus == "0").FirstOrDefault();

                    if (yurticiShipmentStatus == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(yurticiShipmentStatus.docId))
                    {
                        continue;
                    }
                    if (order.OrderNumber == yurticiShipmentStatus.fieldValue) {
                        order.CargoTrackNumber = yurticiShipmentStatus.docId;
                        order.CargoTrackUrl = yurticiShipmentStatus.trackingUrl;
                        order.ShipmentDate = DateTime.TryParseExact(yurticiShipmentStatus.documentDate + yurticiShipmentStatus.documentTime, "yyyyMMddHHmmss", null, DateTimeStyles.None, out var shipmentDateTime)
                            ? shipmentDateTime.AddHours(-3)
                            : DateTime.UtcNow;
                        order.OrderStatusType = OrderStatusType.OrderinCargo;
                        order.CargoRequestHandled = true;

                        updatedOrders.Add(order);
                    }
                  
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