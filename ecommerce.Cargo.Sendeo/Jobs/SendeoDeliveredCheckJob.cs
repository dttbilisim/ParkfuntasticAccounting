using System.Globalization;
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
public class SendeoDeliveredCheckJob : IAsyncBackgroundJob
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    private readonly ILogger _logger;
    private readonly IEmailService _emailService;

    public SendeoDeliveredCheckJob(
        IUnitOfWork<ApplicationDbContext> context,
        SendeoClient sendeoClient,
        ILogger logger,IEmailService emailService)
    {
        _context = context;
        _sendeoClient = sendeoClient;
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
                     && o.Cargo.Name.ToLower().Contains("sendeo")
                     && o.DeliveryDate==null
                   
                    
            )
            .ToListAsync();

        var updatedOrders = new List<Orders>();

        foreach (var orderChunks in deliveryWaitingOrders)
        {
            Delivery shipmentStatuses = new();

            try
            {
                shipmentStatuses = await _sendeoClient.GetDeliveryAsync(orderChunks.CargoTrackNumber.ToString()!);
                _logger.LogInformation($"Sendeo Delivery Response for {orderChunks.CargoTrackNumber}: {Newtonsoft.Json.JsonConvert.SerializeObject(shipmentStatuses)}");

                if (shipmentStatuses.State != (int)SendeoCargoStatus.TeslimEdildi)
                {
                    continue;
                }
                if (shipmentStatuses == null) {
                    continue;
                }
                if (shipmentStatuses.ReferenceNo==orderChunks.OrderNumber) {
                    orderChunks.DeliveryDate = DateTime.TryParseExact(shipmentStatuses.UpdateDate, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deliveryDate)
                  ? deliveryDate
                  : DateTime.Now;
                    orderChunks.DeliveryTo = shipmentStatuses.DeliveryDescription;
                    orderChunks.OrderStatusType = OrderStatusType.OrderSuccess;
                    orderChunks.IsSellerApproved = true;

                    _orderRepository.Update(orderChunks);
                    await _context.SaveChangesAsync();
                    await _emailService.SendOrderShippedEmail(orderChunks);
                }
             
              
            }
            catch (Exception e)
            {
               

                _logger.LogError(e, "Error while getting shipment status from Sendeo API");
                continue;
            }

               

               

               
            }

           
        }

       
       
    
}
