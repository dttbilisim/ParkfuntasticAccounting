using System.IO.Hashing;
using System.Text;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Mng.Enums;
using ecommerce.Cargo.Mng.Models;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DeliveryType = ecommerce.Cargo.Mng.Models.DeliveryType;
using PaymentType = ecommerce.Cargo.Mng.Models.PaymentType;

namespace ecommerce.Cargo.Mng.Jobs;

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(MngReturnOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class MngReturnOrderCreateJob : IAsyncBackgroundJob<MngReturnOrderCreateJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    public MngReturnOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, MngClient mngClient)
    {
        _context = context;
        _mngClient = mngClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(MngReturnOrderCreateJobArgs args)
    {
        var order = await _orderRepository.GetAll(false)
            .Include(o => o.ApplicationUser)
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Include(o => o.Cargo)
            .Where(
                o => o.Id == args.OrderId
                     && o.ReturnCargoExternalId == null
                     && o.ProblemStatus == OrderProblemStatus.CreatingReturnShipment
                     && o.Cargo != null
                     && o.Cargo.Name.ToLower().Contains("mng")
            )
            .FirstOrDefaultAsync();

        if (order == null)
        {
            return;
        }

        var referencePayload = $"{order.OrderNumber}|{order.ReturnOrCancelDate:O}";
        var returnOrderReference = string.Concat(XxHash64.Hash(Encoding.UTF8.GetBytes(referencePayload)).Select(x => x.ToString("X2")));

        order.ReturnCargoReference = returnOrderReference;

        var mngOrderRequest = new CreateReturnOrderRequest
        {
            Order = new Order
            {
                ReferenceId = order.ReturnCargoReference,
                Barcode = order.ReturnCargoReference,
                ShipmentServiceType = ShipmentServiceType.StandartTeslimat,
                PackagingType = PackagingType.Koli,
                // SmsDestinationBranch
                SmsPreference1 = 0,
                // SmsPrepare
                SmsPreference2 = 0,
                // SmsDelivered
                SmsPreference3 = 0,
                PaymentType = PaymentType.PlatformOder,
                DeliveryType = DeliveryType.AdreseTeslim
            },
            Shipper = new Customer
            {
                RefCustomerId = order.Seller.Id.ToString(),
                CityName = "Antalya",
                DistrictName = "Merkez",
                Address = "Antalya",
                Email = order.ApplicationUser?.Email ?? "",
                FullName = order.ApplicationUser?.FullName ?? "",
                MobilePhoneNumber = order.Seller.PhoneNumber
            },
            OrderPieceList = new List<OrderPiece>()
        };

        foreach (var item in order.OrderItems)
        {
            mngOrderRequest.Order.Content = string.IsNullOrEmpty(mngOrderRequest.Order.Content) ? item.Product.Name : mngOrderRequest.Order.Content + ", " + item.Product.Name;
            mngOrderRequest.Order.Description = string.IsNullOrEmpty(mngOrderRequest.Order.Description) ? item.Product.Name : mngOrderRequest.Order.Description + ", " + item.Product.Name;

            var total = item.Width * item.Height * item.Length;

            var desi = (total > 0 ? total / 3000 : 0) * item.Quantity;

            if (desi < 1)
                desi = 1;

            mngOrderRequest.OrderPieceList.Add(
                new OrderPiece
                {
                    Barcode = item.Product.Barcode,
                    Desi = Convert.ToInt32(desi),
                    Kg = Convert.ToInt32(item.Product.Weight * item.Quantity),
                    Content = item.Product.Name
                }
            );
        }

        var mngOrder = await _mngClient.CreateReturnOrderAsync(mngOrderRequest);

        if (mngOrder?.orderInvoiceId.IsNullOrEmpty() ?? true)
        {
            throw new Exception("Could not create mng return order");
        }

        order.ReturnCargoExternalId = mngOrder.orderInvoiceId;
        order.ProblemStatus = OrderProblemStatus.WaitingCargoShipment;

        _orderRepository.Update(order);

        await _context.SaveChangesAsync();
    }
}