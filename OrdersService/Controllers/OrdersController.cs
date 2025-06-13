using Microsoft.AspNetCore.Mvc;
using OrdersService.Services;

namespace OrdersService.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _orderService;

        public OrdersController(OrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromHeader(Name = "user_id")] Guid userId, [FromBody] CreateOrderRequest request)
        {
            var order = await _orderService.CreateOrderAsync(userId, request.Amount, request.Description);
            return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, new { order.Id, order.Status });
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromHeader(Name = "user_id")] Guid userId)
        {
            var orders = await _orderService.GetOrdersAsync(userId);
            return Ok(orders.Select(o => new { o.Id, o.Status, o.Amount, o.Description, o.CreatedAt }));
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(Guid orderId, [FromHeader(Name = "user_id")] Guid userId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null || order.UserId != userId)
                return NotFound();

            return Ok(new { order.Id, order.Status, order.Amount, order.Description, order.CreatedAt });
        }
    }

    public record CreateOrderRequest(decimal Amount, string Description);
}