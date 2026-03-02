using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Core.Utils;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Sipariş geçmişi ve yönetimi
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly IUserOrderService _userOrderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IUserOrderService userOrderService,
        ILogger<OrderController> logger)
    {
        _userOrderService = userOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcının sipariş geçmişini getirir (sayfalı, filtrelenebilir).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserOrderHistoryDto))]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatusType? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _userOrderService.GetUserOrderHistoryAsync(status, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Siparişi iptal eder (sadece bekleyen siparişler).
    /// </summary>
    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId, [FromBody] CancelOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Description))
        {
            return BadRequest(new { success = false, message = "İptal nedeni zorunludur" });
        }

        var (success, message) = await _userOrderService.CancelOrder(orderId, request.Description);
        if (success)
        {
            return Ok(new { success = true, message });
        }
        return BadRequest(new { success = false, message });
    }

    /// <summary>
    /// Giriş yapan kullanıcının belirli bir ürün için geçmiş alışveriş kayıtlarını döner (tarih, sipariş no, miktar, tutar).
    /// </summary>
    [HttpGet("product/{productId:int}/history")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ProductPurchaseHistoryItemDto>))]
    public async Task<IActionResult> GetProductPurchaseHistory(int productId)
    {
        var list = await _userOrderService.GetProductPurchaseHistoryAsync(productId);
        return Ok(list);
    }

    /// <summary>
    /// Giriş yapan kullanıcının daha önce sipariş verdiği ürün ID'lerini döner (ürün listesinde "geçmiş alışveriş" ikonu için).
    /// </summary>
    [HttpGet("purchased-product-ids")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<int>))]
    public async Task<IActionResult> GetPurchasedProductIds([FromQuery] int[] productIds)
    {
        if (productIds == null || productIds.Length == 0)
            return Ok(new List<int>());
        var ids = await _userOrderService.GetPurchasedProductIdsAsync(productIds);
        return Ok(ids);
    }
}

public class CancelOrderRequest
{
    public string Description { get; set; } = string.Empty;
}
