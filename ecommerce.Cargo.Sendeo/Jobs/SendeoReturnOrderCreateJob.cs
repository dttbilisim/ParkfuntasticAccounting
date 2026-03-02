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

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(SendeoReturnOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class SendeoReturnOrderCreateJob : IAsyncBackgroundJob<SendeoReturnOrderCreateJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly SendeoClient _sendeoClient;

    private readonly IRepository<Orders> _orderRepository;

    public SendeoReturnOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, SendeoClient sendeoClient)
    {
        _context = context;
        _sendeoClient = sendeoClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(SendeoReturnOrderCreateJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
         
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.ReturnCargoExternalId == null
                     && o.ProblemStatus == OrderProblemStatus.CreatingReturnShipment
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
          //  sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, order.Seller.Town.Name);
        }
        catch
        {
            //sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Seller.City.Name, "MERKEZ");
        }

        City? sendeoReceiverCity;
        try
        {
           // sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Company.City.Name, order.Company.Town.Name);
        }
        catch
        {
           // sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(order.Company.City.Name, "MERKEZ");
        }

        var sendeoRequest = new SetDeliveryRequest
        {
            DeliveryType = DeliveryType.FromCustomer,
            ReferenceNo = order.OrderNumber,
           // Sender = order.Company.AccountName ?? order.Company.FirstName + " " + order.Company.LastName,
           // SenderAddress = order.Company.Address,
           // SenderCityId = sendeoReceiverCity?.CityId ?? 0,
           // SenderDistrictId = sendeoReceiverCity?.Districts.FirstOrDefault()?.DistrictId ?? 0,
           // SenderGSM = order.Company.PhoneNumber,
           // SenderEmail = order.Company.EmailAddress,
            // SenderTaxpayerId = order.Seller.TaxNumber,
           // Receiver = order.Seller.AccountName ?? order.Seller.FirstName + " " + order.Seller.LastName,
           // ReceiverAuthority = order.Seller.FirstName + " " + order.Seller.LastName,
           // ReceiverAddress = order.Seller.Address,
           // ReceiverCityId = sendeoSellerCity?.CityId ?? 0,
           // ReceiverDistrictId = sendeoSellerCity?.Districts.FirstOrDefault()?.DistrictId ?? 0,
            ReceiverGSM = order.Seller.PhoneNumber,
           // ReceiverEmail = order.Seller.EmailAddress,
            // SenderTaxpayerId = order.Seller.TaxNumber,
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

        order.ReturnCargoExternalId = sendeoDelivery.TrackingNumber;
        order.ReturnCargoTrackNumber = sendeoDelivery.TrackingNumber;
        order.ReturnCargoTrackUrl = sendeoDelivery.TrackingUrl;
        order.ProblemStatus = OrderProblemStatus.WaitingCargoShipment;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}