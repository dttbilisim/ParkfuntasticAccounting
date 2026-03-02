using System.Globalization;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSOrderStatusWebServices;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Cargo.Yurtici.Jobs;

[DisableConcurrentExecution(20 * 60)]
[DisableMultipleQueuedItems]
[AutomaticRetry(Attempts = 0)]
public class YurticiReturnDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public YurticiReturnDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        YurticiClient yurticiClient,
        ILogger logger)
    {
        _context = context;
        _yurticiClient = yurticiClient;
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
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
                     && o.ReturnShipmentDate > DateTime.UtcNow.AddDays(-14)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders.Chunk(50))
        {
            var shipmentResult = await _yurticiClient.GetShipmentStatusAsync(
                new listInvDocumentInterfaceByReference()
                {
                    withCargoLifecycle = "1",
                    fieldName = "3",
                    fieldValueArray = orderChunks.Select(o => o.OrderNumber.ToString()).ToArray(),
                }
            );
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

                 
                    if (!new[] { "1", "2" }.
                        Contains(yurticiShipmentStatus.returnStatus))
                    {
                        continue;
                    }

                    order.ReturnDeliveryDate = DateTime.TryParseExact(yurticiShipmentStatus.returnDeliveryDate, "yyyyMMdd",
                        null, DateTimeStyles.None, out var shipmentDateTime)
                        ? shipmentDateTime.AddHours(-3)
                        : DateTime.UtcNow;
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