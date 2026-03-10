using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Sendeo.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using City = ecommerce.Cargo.Sendeo.Models.City;

namespace ecommerce.Cargo.Sendeo.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(SendeoOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class SendeoOrderCreateJob : IAsyncBackgroundJob<SendeoOrderCreateJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    public SendeoOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, SendeoClient sendeoClient)
    {
        _context = context;
        _sendeoClient = sendeoClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(SendeoOrderCreateJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
        
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Cargo)
            .Include(x=>x.UserAddress)
            .Include(x=>x.Seller).ThenInclude(x=>x.City)
            .Where(
                o => o.Id == args.OrderId
                     && o.CargoExternalId == null
                     && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew)
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("sendeo")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

        City? sendeoSellerCity;
        try
        {
            sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, order.Seller.Town.Name);
        }
        catch
        {
           sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, "MERKEZ");
        }

        City? sendeoReceiverCity;
        try
        {
            sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, order.Seller.Town.Name);
        }
        catch
        {
            sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, "MERKEZ");
        }

        var sendeoRequest = new SetDeliveryRequest
        {
            DeliveryType = DeliveryType.FromSupplier,
            ReferenceNo = order.OrderNumber,
           Sender = order.Seller.Name,
            SenderAuthority = order.Seller.Name,
            SenderAddress = order.Seller.Address,
            SenderCityId = sendeoSellerCity?.CityId ?? 0,
            SenderDistrictId = sendeoSellerCity?.Districts.FirstOrDefault()?.DistrictId ?? 0,
            SenderGSM = order.Seller.PhoneNumber,
            SenderEmail = order.Seller.Email,
            SenderTaxpayerId = order.Seller.TaxNumber,
            
           Receiver = order.UserAddress.FullName,
           ReceiverAuthority = order.UserFullName ?? "",
            ReceiverAddress = order.UserAddress.Address,
           ReceiverCityId = sendeoReceiverCity?.CityId ?? 0,
           ReceiverDistrictId = sendeoReceiverCity?.Districts.FirstOrDefault()?.DistrictId ?? 0, 
           ReceiverGSM = order.UserAddress.PhoneNumber,
           ReceiverEmail = order.UserAddress.Email,
                
            
            

            Products = new List<SetDeliveryProductRequest>()
        };

        var totalDesi = 0m;
        var totalKg = 0m;

        foreach (var item in order.OrderItems)
        {
            var total = item.Width * item.Height * item.Length;
            var desi = (total > 0 ? total / 3000 : 0) * item.Quantity;
            var kg = item.Product.Weight > 0 ? item.Product.Weight / 1000 : 0;

            totalDesi += desi;
            totalKg += kg;
        }

        sendeoRequest.Products.Add(
            new SetDeliveryProductRequest
            {
                Count = 1,
                Deci = totalDesi > 1 ? Convert.ToInt32(totalDesi) : 1,
                Weigth = Convert.ToInt32(totalKg),
                Price = order.CargoPrice
            }
        );

        var sendeoDelivery = await _sendeoClient.SetDeliveryAsync(sendeoRequest);

        if (sendeoDelivery.TrackingNumber == null)
        {
            throw new Exception("Could not create sendeo delivery");
        }

        order.CargoExternalId = sendeoDelivery.TrackingNumber;
        order.CargoTrackNumber = sendeoDelivery.TrackingNumber;
        order.CargoTrackUrl = sendeoDelivery.TrackingUrl;
        order.CargoRequestHandled = true;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}