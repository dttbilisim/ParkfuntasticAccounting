using System.Globalization;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSOrderStatusWebServices;
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
public class YurticiDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly YurticiClient _yurticiClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;
    private readonly IEmailService _emailService;

    public YurticiDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        YurticiClient yurticiClient,
        ILogger logger,IEmailService emailService)
    {
        _context = context;
        _yurticiClient = yurticiClient;
        _logger = logger;
        _emailService = emailService;
        _orderRepository = _context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync()
    {
        var deliveryWaitingOrders = await _orderRepository.GetAll(false)
            .Include(o => o.Cargo)
            .Where(
                o => o.CargoTrackUrl != null
                     && o.CargoTrackNumber != null
                     && o.OrderStatusType == OrderStatusType.OrderinCargo
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("yurtiçi")
                     && o.DeliveryDate==null
                    

            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders.Chunk(50))
        {
            //TODO: Kaan - bu docIdArray doğrumu kontrol edilecek, 
            var shipmentResult = await _yurticiClient.GetShipmentStatusAsync(
                new KOPSOrderStatusWebServices.listInvDocumentInterfaceByReference
                {
                    //docIdArray = orderChunks.Select(o => o.OrderNumber).ToArray()
                    withCargoLifecycle = "1",
                    fieldName = "3",
                    fieldValueArray = orderChunks.Select(o => o.OrderNumber.ToString()).ToArray(),
                }
            );
            var shipmentStatuses = shipmentResult.ShippingDataResponseVO;

            try
            {
                var yurticiShipmentStatus = shipmentStatuses.shippingDataDetailVOArray.Where(f => f.transactionStatus == "0" && f.receiverInfo!=null);

                if (yurticiShipmentStatus == null)
                {
                    continue;
                }

                foreach (var item in yurticiShipmentStatus)
                {
                    var order = orderChunks.FirstOrDefault(x => x.OrderNumber == item.fieldValue);
                    if (order != null)
                    {
                        order.DeliveryDate = DateTime.TryParseExact(item.deliveryDate + item.deliveryTime, "yyyyMMddHHmmss", null, DateTimeStyles.None, out var shipmentDateTime)
                       ? shipmentDateTime.AddHours(-3)
                       : DateTime.UtcNow;
                        order.DeliveryTo = item.receiverInfo;
                        order.OrderStatusType = OrderStatusType.OrderSuccess;
                        order.IsSellerApproved = true;
                        order.CargoTrackNumber = item.docId;
                        order.ReturnCargoTrackUrl = item.trackingUrl;

                        updatedOrders.Add(order);
                        await _emailService.SendOrderShippedEmail(order);
                        if (updatedOrders.Any())
                        {
                            _orderRepository.Update(updatedOrders);
                        }

                        await _context.SaveChangesAsync();
                    }
                }



            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error while updating shipment delivery order id:, trackingNumber:");
            }
        }



    }
}
