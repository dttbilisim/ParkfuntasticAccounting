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
public class YurticiReturnReadyCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;

    public YurticiReturnReadyCheckJob(
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
        var waitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.ReturnCargoExternalId != null
                     && o.ReturnCargoTrackNumber == null
                     && o.ProblemStatus == OrderProblemStatus.WaitingCargoShipment
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
                     && o.ReturnOrCancelDate > DateTime.UtcNow.AddDays(-7)
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in waitingOrders.Chunk(50))
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
                    var yurticiShipmentStatus = shipmentStatuses.shippingDataDetailVOArray.Where(f=>f.transactionStatus == "0").FirstOrDefault();

                    if (yurticiShipmentStatus == null)
                    {
                        continue;
                    }

                    order.ReturnCargoTrackNumber = yurticiShipmentStatus.returnDocId;
                    order.ReturnCargoTrackUrl = yurticiShipmentStatus.trackingUrl?.
                        Replace(yurticiShipmentStatus.docId, 
                        yurticiShipmentStatus.returnDocId);
                    order.ReturnShipmentDate = DateTime.TryParseExact(yurticiShipmentStatus.returnDocumentDate, "yyyyMMdd", null,
                        DateTimeStyles.None, out var shipmentDateTime)
                        ? shipmentDateTime.AddHours(-3)
                        : DateTime.UtcNow;
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