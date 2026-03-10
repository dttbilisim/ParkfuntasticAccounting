using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
namespace ecommerce.Cargo.Yurtici.Jobs;
[DisableConcurrentExecutionWithRetry(5 * 60, $"{{{nameof(YurticiOrderCreateJobArgs.OrderId)}}}", MaxRetryAttempts = 5)]
public class YurticiOrderCreateJob : IAsyncBackgroundJob<YurticiOrderCreateJobArgs>{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly YurticiClient _yurticiClient;
    private readonly IRepository<Orders> _orderRepository;
    public YurticiOrderCreateJob(IUnitOfWork<ApplicationDbContext> context, YurticiClient yurticiClient){
        _context = context;
        _yurticiClient = yurticiClient;
        _orderRepository = context.GetRepository<Orders>();
    }
    
    public async Task ExecuteAsync(YurticiOrderCreateJobArgs args){
        var order = await _orderRepository.GetAll(false)
            .Include(o => o.ApplicationUser)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.Cargo)
            .Where(o => o.Id == args.OrderId && o.CargoExternalId == null && (o.OrderStatusType == OrderStatusType.OrderPrepare || o.OrderStatusType == OrderStatusType.OrderNew) && o.Cargo != null && o.Cargo.Name.ToLower().Contains("yurtiçi"))
            .FirstOrDefaultAsync();
        if(order == null){
            return;
        }
        var cargoData = new XDocCargoData[1];
        cargoData[0]=new XDocCargoData { ngiCargoKey = order.OrderNumber,cargoType = "2",cargoCount = 1 };

        var specialFieldData = new XSpecialFieldData[1];
        specialFieldData[0]=new XSpecialFieldData { specialFieldName = "3",specialFieldValue = order.OrderNumber};
       
        var yurticiRequest = new XShipmentData{
            totalDesi = 0,
            totalWeight = 0,
            totalCargoCount = 1,
            cargoType = "2",
            ngiDocumentKey = order.OrderNumber,
            productCode = "STA",
            personGiver = "ECZAPRO",
            description = "",
            docCargoDataArray= cargoData,
            specialFieldDataArray = specialFieldData,
            
            
        };

        //Company:Alıcı => Consignee
        //Gönderici:Seller => 
        var yurticiSender = new XSenderCustAddress(){
           // cityId = order.Seller.City.Id.ToString(),
            senderMobilePhone = order.Seller.PhoneNumber,
           // senderEmailAddress = order.Seller.EmailAddress,
           // senderCustName = order.Seller.FirstName + " " + order.Seller.LastName,
           // senderAddress = string.IsNullOrEmpty(order.Seller.Address) ? order.Seller.InvoiceAddress+"/"+order.Seller.City.Name+"/"+order.Seller.Town.Name : order.Seller.Address+"/"+order.Seller.City.Name+"/"+order.Seller.Town.Name,
            senderPhone = order.Seller.PhoneNumber,
           // townName = order.Seller.Town.Name
            
        };
        var yurticiReceiver = new XConsigneeCustAddress(){
            // consigneeCustName = order.Company.FirstName + " " + order.Company.LastName,
            // consigneeMobilePhone = order.Company.PhoneNumber,
            // consigneeEmailAddress = order.Company.EmailAddress,
            // consigneeAddress = string.IsNullOrEmpty(order.Company.Address) ? order.Company.InvoiceAddress+"/"+order.Company.City.Name+"/"+order.Company.Town.Name : order.Company.Address+"/"+order.Company.City.Name+"/"+order.Company.Town.Name,
            // townName = order.Company.Town.Name,
            // cityId = order.Company.City.Id.ToString()
            
        };
        var yurticiPayer = new XPayerCustData(){invCustId = YurticiClientConstants.CustId,invAddressId ="" };
        var totalDesi = 0m;
        var totalKg = 0m;
        foreach(var item in order.OrderItems){
            var total = item.Width * item.Height * item.Length;
            var desi = (total > 0 ? total / 3000 : 0) * item.Quantity;
            var kg = item.Product.Weight > 0 ? item.Product.Weight / 1000 : 0;
            totalDesi += desi;
            totalKg += kg;
        }
        yurticiRequest.totalDesi = Convert.ToDouble(totalDesi);
        yurticiRequest.totalWeight = Convert.ToDouble(totalKg);
       // yurticiRequest.totalCargoCount = order.OrderItems.Count;
        
    
           
        var yurticiResponse = await _yurticiClient.CreateOrderAsync(new createNgiShipmentWithAddress{
                shipmentData = yurticiRequest,
                XSenderCustAddress = yurticiSender, 
                XConsigneeCustAddress = yurticiReceiver,
                payerCustData = yurticiPayer
                
                
                
                
            }
        );
        if(yurticiResponse?.outFlag != "0") // başarılı değilse
        {
            throw new Exception("Could not create yurtici delivery: " + yurticiResponse.outResult ?? yurticiResponse.errCode);
        }

        //yurticiResponse.specialFieldDataArray.Where(f=>)
        order.CargoExternalId = yurticiResponse.outResult;
        order.CargoRequestHandled = true;
        _orderRepository.Update(order);
        await _context.SaveChangesAsync();
    }
}
