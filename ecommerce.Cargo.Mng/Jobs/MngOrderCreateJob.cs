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

[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(MngOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class MngOrderCreateJob : IAsyncBackgroundJob<MngOrderCreateJobArgs>
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;

    private readonly MngClient _mngClient;

    private readonly IRepository<Orders> _orderRepository;

    public MngOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, MngClient mngClient)
    {
        _context = context;
        _mngClient = mngClient;

        _orderRepository = context.GetRepository<Orders>();
    }

    public async Task ExecuteAsync(MngOrderCreateJobArgs args)
    {
        try
        {
            var order = await _orderRepository.GetAll(false)
                .Include(o => o.ApplicationUser)
                .Include(o => o.Seller)
                .Include(o => o.OrderItems).ThenInclude(i => i.Product)
                .Include(o => o.Cargo)
                .Where(o => o.Id == args.OrderId && o.CargoExternalId == null && o.OrderStatusType == OrderStatusType.OrderPrepare && o.Cargo != null && o.Cargo.Name.ToLower().Contains("mng"))
                .FirstOrDefaultAsync();

            if(order == null){
                return;
            }

            var mngOrderRequest = new CreateDetailOrder{
                Order = new Order{
                    ReferenceId = order.OrderNumber,
                    Barcode = order.OrderNumber,
                    ShipmentServiceType = ShipmentServiceType.StandartTeslimat,
                    PackagingType = PackagingType.Koli,
                    // SmsDestinationBranch
                    SmsPreference1 = 0,
                    // SmsPrepare
                    SmsPreference2 = 0,
                    // SmsDelivered
                    SmsPreference3 = 0,
                    PaymentType = PaymentType.PlatformOder,
                    DeliveryType = DeliveryType.AdreseTeslim,
                    MarketPlaceShortCode = "",
                    MarketPlaceSaleCode = ""
                 
                },
                Recipient = new Recipient(){
                    RefCustomerId = order.ApplicationUser?.Id.ToString() ?? order.CompanyId.ToString(),
                    CityName = "Antalya",
                    DistrictName = "Merkez",
                    Address = "Antalya",
                    Email = order.ApplicationUser?.Email ?? "",
                    FullName = order.ApplicationUser?.FullName ?? "",
                    MobilePhoneNumber = order.ApplicationUser?.PhoneNumber ?? "",
                    HomePhoneNumber = order.ApplicationUser?.PhoneNumber ?? "",
                    TaxNumber = "addadasdad",
                    TaxOffice ="Antalya"
                },
                Shipper = new Shipper{
                    RefCustomerId = order.Seller.Id.ToString(),
                    CityName = "Antalya",
                    DistrictName = "Merkez",
                    Address = "ANTALA",
                    Email = order.Seller.Email,
                    FullName = order.Seller.Name,
                    MobilePhoneNumber = order.Seller.PhoneNumber,
                    HomePhoneNumber=order.Seller.PhoneNumber,
                    TaxNumber = "123131",
                    TaxOffice ="Antalya"
                },
                OrderPieceList = new List<OrderPiece>()
            };

            foreach(var item in order.OrderItems){
                mngOrderRequest.Order.Barcode = item.Product.Barcode;
                mngOrderRequest.Order.Content =item.Product.Name.Length>=200?item.Product.Name.Substring(1,200):item.Product.Name;

                mngOrderRequest.Order.Description = item.Product.Description.Length >= 200 ? item.Product.Description.Substring(1,200) : item.Product.Description;
                    //string.IsNullOrEmpty(mngOrderRequest.Order.Description) ? item.Product.Name : mngOrderRequest.Order.Description + ", " + item.Product.Name;

                var total = item.Width * item.Height * item.Length;

                var desi = (total > 0 ? total / 3000 : 0) * item.Quantity;

                if(desi < 1) desi = 1;

                mngOrderRequest.OrderPieceList.Add(new OrderPiece{Barcode = item.Product.Barcode, Desi = Convert.ToInt32(desi), Kg = Convert.ToInt32(item.Product.Weight * item.Quantity), Content = item.Product.Name});
            }

            var mngOrder = (await _mngClient.CreateOrderAsync(mngOrderRequest));

            
            if(mngOrder.orderInvoiceId==null){
                throw new Exception(mngOrder.Error.Description);
            }

            order.CargoExternalId = mngOrder.orderInvoiceId;
            order.CargoRequestHandled = true;

            _orderRepository.Update(order);

            await _context.SaveChangesAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}