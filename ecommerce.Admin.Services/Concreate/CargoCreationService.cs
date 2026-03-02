using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Cargo.Sendeo;
using ecommerce.Cargo.Sendeo.Models;
using ecommerce.Cargo.Yurtici;
using ecommerce.Cargo.Yurtici.KOPSWebServices;
using ecommerce.Cargo.Mng;
using ecommerce.Cargo.Mng.Models;
using ecommerce.Cargo.Mng.Enums;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Domain.Concreate
{
    public class CargoCreationService : ICargoCreationService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _context;
        private readonly SendeoClient _sendeoClient;
        private readonly YurticiClient? _yurticiClient;
        private readonly MngClient? _mngClient;
        private readonly IRepository<Orders> _orderRepository;

        public CargoCreationService(
            IUnitOfWork<ApplicationDbContext> context,
            SendeoClient sendeoClient,
            YurticiClient? yurticiClient = null,
            MngClient? mngClient = null)
        {
            _context = context;
            _sendeoClient = sendeoClient;
            _yurticiClient = yurticiClient;
            _mngClient = mngClient;
            _orderRepository = context.GetRepository<Orders>();
        }

        public async Task<CargoCreationResultDto> CreateCargoForWarehouseGroupAsync(
            int orderId, 
            string warehouseName, 
            List<int> productIds)
        {
            try
            {
                // Load order with all necessary data
                var order = await _orderRepository.GetAll(false)
                    .Include(o => o.OrderItems)
                        .ThenInclude(i => i.Product)
                    .Include(o => o.Cargo)
                    .Include(o => o.ApplicationUser)
                    .Include(o => o.UserAddress)
                        .ThenInclude(ua => ua.City)
                    .Include(o => o.UserAddress)
                        .ThenInclude(ua => ua.Town)
                    .Include(o => o.Seller)
                        .ThenInclude(s => s.City)
                    .Include(o => o.Seller)
                        .ThenInclude(s => s.Town)
                    .Where(o => o.Id == orderId && o.Cargo != null)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı veya kargo tanımlı değil"
                    };
                }

                // Filter items for this warehouse
                var warehouseItems = order.OrderItems
                    .Where(i => productIds.Contains(i.Id))
                    .ToList();

                if (!warehouseItems.Any())
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = $"{warehouseName} deposu için ürün bulunamadı"
                    };
                }

                // Fetch warehouse specific address
                var sellerAddressRepository = _context.GetRepository<SellerAddress>();
                var allAddresses = await sellerAddressRepository.GetAll(false)
                    .Include(sa => sa.City)
                    .Include(sa => sa.Town)
                    .Where(sa => sa.SellerId == order.SellerId)
                    .ToListAsync();

                var warehouseAddress = allAddresses.FirstOrDefault(sa => 
                    sa.Title == warehouseName || 
                    (!string.IsNullOrEmpty(sa.StockWhereIs) && sa.StockWhereIs.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                        .Any(s => string.Equals(s.Trim(), warehouseName, StringComparison.OrdinalIgnoreCase))));

                // Determine cargo provider
                var cargoName = order.Cargo.Name.ToLower();

                if (cargoName.Contains("sendeo"))
                {
                    return await CreateSendeoCargoAsync(order, warehouseName, warehouseItems, warehouseAddress);
                }
                else if (cargoName.Contains("yurtiçi") || cargoName.Contains("yurtici"))
                {
                    return await CreateYurticiCargoAsync(order, warehouseName, warehouseItems, warehouseAddress);
                }
                else if (cargoName.Contains("mng"))
                {
                    return await CreateMngCargoAsync(order, warehouseName, warehouseItems, warehouseAddress);
                }
                else
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = $"Desteklenmeyen kargo firması: {order.Cargo.Name}",
                        CargoProvider = order.Cargo.Name
                    };
                }
            }
            catch (Exception ex)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = $"Kargo oluşturma hatası: {ex.Message}"
                };
            }
        }

        private async Task<CargoCreationResultDto> CreateSendeoCargoAsync(
            Orders order, 
            string warehouseName, 
            List<OrderItems> items,
            SellerAddress? warehouseAddress)
        {
            try
            {
                // Prepare Seller Address Info
                string sellerAddressText = order.Seller.Address;
                string sellerCityName = order.Seller.City?.Name ?? "";
                string sellerTownName = order.Seller.Town?.Name ?? "MERKEZ";
                string sellerPhone = order.Seller.PhoneNumber;
                string sellerEmail = order.Seller.Email;

                if (warehouseAddress != null)
                {
                    sellerAddressText = warehouseAddress.Address;
                    sellerCityName = warehouseAddress.City?.Name ?? sellerCityName;
                    sellerTownName = warehouseAddress.Town?.Name ?? sellerTownName;
                    sellerPhone = !string.IsNullOrEmpty(warehouseAddress.PhoneNumber) ? warehouseAddress.PhoneNumber : sellerPhone;
                    sellerEmail = !string.IsNullOrEmpty(warehouseAddress.Email) ? warehouseAddress.Email : sellerEmail;
                }

                // Get cities for Sendeo
                Cargo.Sendeo.Models.City? sendeoSellerCity;
                try
                {
                    sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(
                        sellerCityName, 
                        sellerTownName);
                }
                catch
                {
                    sendeoSellerCity = await _sendeoClient.GetCityWithDistrictsAsync(
                        sellerCityName, 
                        "MERKEZ");
                }

                Cargo.Sendeo.Models.City? sendeoReceiverCity;
                try
                {
                    // Add null checks for safety
                    var receiverCityName = order.UserAddress?.City?.Name ?? "İstanbul";
                    var receiverTownName = order.UserAddress?.Town?.Name ?? "MERKEZ";
                    
                    sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(
                        receiverCityName, 
                        receiverTownName);
                }
                catch
                {
                    // Fallback to default city with MERKEZ district
                    sendeoReceiverCity = await _sendeoClient.GetCityWithDistrictsAsync(
                        order.UserAddress?.City?.Name ?? "İstanbul", 
                        "MERKEZ");
                }

                // Calculate total desi and weight for warehouse items
                var totalDesi = 0m;
                var totalKg = 0m;

                foreach (var item in items)
                {
                    decimal desi = 0;
                    if (item.Product.CargoDesi > 0)
                    {
                        desi = item.Product.CargoDesi * item.Quantity;
                    }
                    else
                    {
                        var total = item.Width * item.Height * item.Length;
                        desi = (total > 0 ? total / 3000 : 0) * item.Quantity;
                    }
                    
                    var kg = item.Product.Weight > 0 ? item.Product.Weight / 1000 : 0;

                    totalDesi += desi;
                    totalKg += kg;
                }

                // Create Sendeo request
                // Safely get Seller district
                int senderDistrictId = 0;
                if (sendeoSellerCity != null && sendeoSellerCity.Districts != null)
                {
                    var district = sendeoSellerCity.Districts.FirstOrDefault();
                    senderDistrictId = district?.DistrictId ?? 0;
                }

                // Safely get Receiver district
                int receiverDistrictId = 0;
                if (sendeoReceiverCity != null && sendeoReceiverCity.Districts != null)
                {
                    var district = sendeoReceiverCity.Districts.FirstOrDefault();
                    receiverDistrictId = district?.DistrictId ?? 0;
                }
                
                // Safely get User Email
                string receiverEmail = order.UserAddress?.Email ?? "";
                if (string.IsNullOrEmpty(receiverEmail))
                {
                    receiverEmail = order.User?.Email ?? "";
                }

                // Create Sendeo request
                var sendeoRequest = new SetDeliveryRequest
                {
                    DeliveryType = Cargo.Sendeo.Models.DeliveryType.FromSupplier,
                    ReferenceNo = $"{order.OrderNumber}-{warehouseName}",
                    Sender = order.Seller.Name ?? "Satici", 
                    SenderAuthority = order.Seller.Name ?? "Yetkili",
                    SenderAddress = sellerAddressText ?? "Adres Yok",
                    SenderCityId = sendeoSellerCity?.CityId ?? 0,
                    SenderDistrictId = senderDistrictId,
                    SenderGSM = sellerPhone ?? "",
                    SenderEmail = sellerEmail ?? "",
                    SenderTaxpayerId = order.Seller.TaxNumber ?? "",
                    
                    Receiver = order.UserAddress?.FullName ?? "Alici",
                    ReceiverAuthority = (order.User?.FirstName ?? "") + " " + (order.User?.LastName ?? ""),
                    ReceiverAddress = order.UserAddress?.Address ?? "Adres Yok",
                    ReceiverCityId = sendeoReceiverCity?.CityId ?? 0,
                    ReceiverDistrictId = receiverDistrictId,
                    ReceiverGSM = order.UserAddress?.PhoneNumber ?? "",
                    ReceiverEmail = receiverEmail,

                    Products = new List<SetDeliveryProductRequest>
                    {
                        new SetDeliveryProductRequest
                        {
                            Count = items.Count,
                            Deci = totalDesi > 1 ? Convert.ToInt32(totalDesi) : 1,
                            Weigth = Convert.ToInt32(totalKg),
                            Price = order.CargoPrice
                        }
                    }
                };

                var sendeoDelivery = await _sendeoClient.SetDeliveryAsync(sendeoRequest);

                if (sendeoDelivery.TrackingNumber == null)
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Sendeo kargo oluşturulamadı",
                        CargoProvider = "Sendeo"
                    };
                }

                // CRITICAL: Save cargo info to OrderItems (not Orders!)
                foreach (var item in items)
                {
                    item.CargoExternalId = sendeoDelivery.TrackingNumber;
                    item.CargoTrackNumber = sendeoDelivery.TrackingNumber;
                    item.CargoTrackUrl = sendeoDelivery.TrackingUrl;
                    item.CargoRequestHandled = true;
                    item.ShipmentDate = DateTime.UtcNow;
                }

                var itemRepository = _context.GetRepository<OrderItems>();
                foreach (var item in items)
                {
                    itemRepository.Update(item);
                }
                await _context.SaveChangesAsync();

                return new CargoCreationResultDto
                {
                    Success = true,
                    Message = $"{warehouseName} deposu için Sendeo kargo başarıyla oluşturuldu",
                    CargoTrackingNumber = sendeoDelivery.TrackingNumber,
                    CargoProvider = "Sendeo"
                };
            }
            catch (Exception ex)
            {
                // Improved error logging
                var errorMsg = $"Sendeo hatası: {ex.Message}";
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = errorMsg,
                    CargoProvider = "Sendeo"
                };
            }
        }

        private async Task<CargoCreationResultDto> CreateYurticiCargoAsync(
            Orders order, 
            string warehouseName, 
            List<OrderItems> items,
            SellerAddress? warehouseAddress)
        {
            if (_yurticiClient == null)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = "Yurtiçi Kargo servisi yapılandırılmamış",
                    CargoProvider = "Yurtiçi Kargo"
                };
            }

            try
            {
                var cargoData = new XDocCargoData[1];
                cargoData[0] = new XDocCargoData 
                { 
                    ngiCargoKey = $"{order.OrderNumber}-{warehouseName}",
                    cargoType = "2",
                    cargoCount = 1 
                };

                var specialFieldData = new XSpecialFieldData[1];
                specialFieldData[0] = new XSpecialFieldData 
                { 
                    specialFieldName = "3",
                    specialFieldValue = order.OrderNumber
                };

                // Calculate desi and weight
                var totalDesi = 0m;
                var totalKg = 0m;
                foreach(var item in items)
                {
                    decimal desi = 0;
                    if (item.Product.CargoDesi > 0)
                    {
                        desi = item.Product.CargoDesi * item.Quantity;
                    }
                    else
                    {
                        var total = item.Width * item.Height * item.Length;
                        desi = (total > 0 ? total / 3000 : 0) * item.Quantity;
                    }

                    var kg = item.Product.Weight > 0 ? item.Product.Weight / 1000 : 0;
                    totalDesi += desi;
                    totalKg += kg;
                }

                var yurticiRequest = new XShipmentData
                {
                    totalDesi = Convert.ToDouble(totalDesi),
                    totalWeight = Convert.ToDouble(totalKg),
                    totalCargoCount = 1,
                    cargoType = "2",
                    ngiDocumentKey = $"{order.OrderNumber}-{warehouseName}",
                    productCode = "STA",
                    personGiver = order.Seller.Name,
                    description = $"{warehouseName} deposu",
                    docCargoDataArray = cargoData,
                    specialFieldDataArray = specialFieldData,
                };

                // Prepare Seller Address Info
                string sellerPhone = order.Seller.PhoneNumber;
                if (warehouseAddress != null)
                {
                    sellerPhone = !string.IsNullOrEmpty(warehouseAddress.PhoneNumber) ? warehouseAddress.PhoneNumber : sellerPhone;
                }

                var yurticiSender = new XSenderCustAddress
                {
                    senderMobilePhone = sellerPhone,
                    senderPhone = sellerPhone,
                    senderAddress = warehouseAddress != null 
                        ? $"{warehouseAddress.Address} {warehouseAddress.City?.Name}" 
                        : null,
                    townName = warehouseAddress?.Town?.Name
                };

                var yurticiReceiver = new XConsigneeCustAddress
                {
                    // Note: Most fields are commented out in the job, keeping same pattern
                };

                var yurticiPayer = new XPayerCustData
                {
                    invCustId = YurticiClientConstants.CustId,
                    invAddressId = ""
                };

                var yurticiResponse = await _yurticiClient.CreateOrderAsync(new createNgiShipmentWithAddress
                {
                    shipmentData = yurticiRequest,
                    XSenderCustAddress = yurticiSender, 
                    XConsigneeCustAddress = yurticiReceiver,
                    payerCustData = yurticiPayer
                });

                if(yurticiResponse?.outFlag != "0")
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = $"Yurtiçi Kargo hatası: {yurticiResponse?.outResult ?? yurticiResponse?.errCode}",
                        CargoProvider = "Yurtiçi Kargo"
                    };
                }

                // CRITICAL: Save cargo info to OrderItems (not Orders!)
                foreach (var item in items)
                {
                    item.CargoExternalId = yurticiResponse.outResult;
                    item.CargoTrackNumber = yurticiResponse.outResult;
                    item.CargoTrackUrl = null; // Yurtici doesn't provide URL
                    item.CargoRequestHandled = true;
                    item.ShipmentDate = DateTime.UtcNow;
                }

                var itemRepository = _context.GetRepository<OrderItems>();
                foreach (var item in items)
                {
                    itemRepository.Update(item);
                }
                await _context.SaveChangesAsync();

                return new CargoCreationResultDto
                {
                    Success = true,
                    Message = $"{warehouseName} deposu için Yurtiçi Kargo başarıyla oluşturuldu",
                    CargoTrackingNumber = yurticiResponse.outResult,
                    CargoProvider = "Yurtiçi Kargo"
                };
            }
            catch (Exception ex)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = $"Yurtiçi Kargo hatası: {ex.Message}",
                    CargoProvider = "Yurtiçi Kargo"
                };
            }
        }

        private async Task<CargoCreationResultDto> CreateMngCargoAsync(
            Orders order, 
            string warehouseName, 
            List<OrderItems> items,
            SellerAddress? warehouseAddress)
        {
            if (_mngClient == null)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = "MNG Kargo servisi yapılandırılmamış",
                    CargoProvider = "MNG Kargo"
                };
            }

            try
            {
                var mngOrderRequest = new CreateDetailOrder
                {
                    Order = new Order
                    {
                        ReferenceId = $"{order.OrderNumber}-{warehouseName}",
                        Barcode = order.OrderNumber,
                        ShipmentServiceType = ShipmentServiceType.StandartTeslimat,
                        PackagingType = PackagingType.Koli,
                        SmsPreference1 = 0,
                        SmsPreference2 = 0,
                        SmsPreference3 = 0,
                        PaymentType = Cargo.Mng.Models.PaymentType.PlatformOder,
                        DeliveryType = Cargo.Mng.Models.DeliveryType.AdreseTeslim,
                        MarketPlaceShortCode = "",
                        MarketPlaceSaleCode = ""
                    },
                    Recipient = new Recipient
                    {
                        RefCustomerId = order.CurrentUser?.Id.ToString() ?? order.CompanyId.ToString(),
                        CityName = order.UserAddress?.City?.Name ?? "Bilinmiyor",
                        DistrictName = order.UserAddress?.Town?.Name ?? "Merkez",
                        Address = order.UserAddress?.Address ?? "",
                        Email = order.UserEmail ?? "",
                        FullName = order.UserFullName ?? "",
                        MobilePhoneNumber = order.UserPhoneNumber ?? "",
                        HomePhoneNumber = order.UserPhoneNumber ?? "",
                        TaxNumber = "",
                        TaxOffice = ""
                    },
                    Shipper = new Shipper
                    {
                        RefCustomerId = order.Seller.Id.ToString(),
                        CityName = warehouseAddress?.City?.Name ?? order.Seller.City?.Name ?? "Bilinmiyor",
                        DistrictName = warehouseAddress?.Town?.Name ?? order.Seller.Town?.Name ?? "Merkez",
                        Address = warehouseAddress?.Address ?? order.Seller.Address,
                        Email = !string.IsNullOrEmpty(warehouseAddress?.Email) ? warehouseAddress.Email : order.Seller.Email,
                        FullName = order.Seller.Name,
                        MobilePhoneNumber = !string.IsNullOrEmpty(warehouseAddress?.PhoneNumber) ? warehouseAddress.PhoneNumber : order.Seller.PhoneNumber,
                        HomePhoneNumber = !string.IsNullOrEmpty(warehouseAddress?.PhoneNumber) ? warehouseAddress.PhoneNumber : order.Seller.PhoneNumber,
                        TaxNumber = order.Seller.TaxNumber ?? "",
                        TaxOffice = ""
                    },
                    OrderPieceList = new List<OrderPiece>()
                };

                foreach(var item in items)
                {
                    var productName = item.Product.Name ?? "Ürün";
                    mngOrderRequest.Order.Barcode = item.Product.Barcode;
                    mngOrderRequest.Order.Content = productName.Length >= 200 
                        ? productName.Substring(0, 200) 
                        : productName;

                    var description = item.Product.Description ?? "";
                    mngOrderRequest.Order.Description = description.Length >= 200 
                        ? description.Substring(0, 200) 
                        : description;

                    decimal desi = 0;
                    if (item.Product.CargoDesi > 0)
                    {
                        desi = item.Product.CargoDesi * item.Quantity;
                    }
                    else
                    {
                        var total = item.Width * item.Height * item.Length;
                        desi = (total > 0 ? total / 3000 : 0) * item.Quantity;
                    }
                    
                    if(desi < 1) desi = 1;

                    mngOrderRequest.OrderPieceList.Add(new OrderPiece
                    {
                        Barcode = item.Product.Barcode,
                        Desi = Convert.ToInt32(desi),
                        Kg = Convert.ToInt32(item.Product.Weight * item.Quantity),
                        Content = productName
                    });
                }

                var mngOrder = await _mngClient.CreateOrderAsync(mngOrderRequest);

                if(mngOrder.orderInvoiceId == null)
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = $"MNG Kargo hatası: {mngOrder.Error?.Description ?? "Bilinmeyen hata"}",
                        CargoProvider = "MNG Kargo"
                    };
                }

                return new CargoCreationResultDto
                {
                    Success = true,
                    Message = $"{warehouseName} deposu için MNG Kargo başarıyla oluşturuldu",
                    CargoTrackingNumber = mngOrder.orderInvoiceId,
                    CargoProvider = "MNG Kargo"
                };
            }
            catch (Exception ex)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = $"MNG Kargo hatası: {ex.Message}",
                    CargoProvider = "MNG Kargo"
                };
            }
        }

        public async Task<CargoCreationResultDto> CancelSendeoCargoAsync(
            int orderId,
            string warehouseName,
            List<int> productIds)
        {
            try
            {
                // Load order items
                var order = await _orderRepository.GetAll(false)
                    .Include(o => o.OrderItems)
                    .Include(o => o.Cargo)
                    .Where(o => o.Id == orderId)
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı"
                    };
                }

                // Filter warehouse items
                var warehouseItems = order.OrderItems
                    .Where(i => productIds.Contains(i.Id))
                    .ToList();

                if (!warehouseItems.Any())
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = $"{warehouseName} deposu için ürün bulunamadı"
                    };
                }

                // Get first item's tracking number (all items in group have same tracking)
                var trackingNumber = warehouseItems.First().CargoTrackNumber;
                if (string.IsNullOrEmpty(trackingNumber))
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Kargo takip numarası bulunamadı"
                    };
                }

                // Call Sendeo cancel API
                var referenceNo = $"{order.OrderNumber}-{warehouseName}";
                
                // Parse tracking number to long
                if (!long.TryParse(trackingNumber, out var trackingNo))
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Geçersiz takip numarası"
                    };
                }

                var cancelled = await _sendeoClient.CancelDeliveryAsync(trackingNo, referenceNo);

                if (!cancelled)
                {
                    return new CargoCreationResultDto
                    {
                        Success = false,
                        Message = "Sendeo kargo iptali başarısız",
                        CargoProvider = "Sendeo"
                    };
                }

                // Reset cargo info in OrderItems
                foreach (var item in warehouseItems)
                {
                    item.CargoExternalId = null;
                    item.CargoTrackNumber = null;
                    item.CargoTrackUrl = null;
                    item.CargoRequestHandled = false;
                    item.ShipmentDate = null;
                }

                var itemRepository = _context.GetRepository<OrderItems>();
                foreach (var item in warehouseItems)
                {
                    itemRepository.Update(item);
                }
                await _context.SaveChangesAsync();

                return new CargoCreationResultDto
                {
                    Success = true,
                    Message = $"{warehouseName} deposu için Sendeo kargo iptal edildi",
                    CargoProvider = "Sendeo"
                };
            }
            catch (Exception ex)
            {
                return new CargoCreationResultDto
                {
                    Success = false,
                    Message = $"İptal hatası: {ex.Message}",
                    CargoProvider = "Sendeo"
                };
            }
        }
    }
}
