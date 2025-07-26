using Jumia_Api.Application.Dtos.OrderDtos;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("getall")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("get-by-id/{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByCustomerId(int customerId)
        {
            var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> CreateOrder(CreateOrderDTO orderDto)
        {
            var createdOrder = await _orderService.CreateOrderAsync(orderDto);
            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.OrderId }, createdOrder);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, UpdateOrderDTO orderDto)
        {
            var result = await _orderService.UpdateOrderAsync(id, orderDto);
            if (result == null)
                return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var success = await _orderService.DeleteOrderAsync(id);
            if (!success)
                return NotFound();

            return NoContent();
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderDTO cancelDto)
        {
            var success = await _orderService.CancelOrderAsync(id, cancelDto?.Reason);
            if (!success)
                return BadRequest("Order cannot be cancelled or already cancelled");

            return NoContent();
        }

        [HttpGet("current-customer")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetCurrentCustomerOrders()
        {
            int customerId = GetCustomerId();

            var orders = await _orderService.GetOrdersByCustomerIdAsync(customerId);
            if (orders == null || !orders.Any())
                return NotFound("No orders found for the current customer.");
            return Ok(orders);
        }
        [HttpGet("suborders/{orderId}")]
        public async Task<ActionResult<IEnumerable<SubOrderDTO>>> GetSubOrdersByOrderId(int orderId)
        {
            var subOrders = await _orderService.GetSubOrdersByOrderIdAsync(orderId);
            if (subOrders == null || !subOrders.Any())
                return NotFound("No suborders found for the specified order.");
            return Ok(subOrders);
        }
        [HttpGet("suborders/seller/{sellerId}")]
        public async Task<ActionResult<IEnumerable<SubOrderDTO>>> GetSubOrdersBySellerId(int sellerId)
        {
            var subOrders = await _orderService.GetSubOrdersBySellerIdAsync(sellerId);
            if (subOrders == null || !subOrders.Any())
                return NotFound("No suborders found for the specified seller.");
            return Ok(subOrders);
        }
        private int GetCustomerId()
        {
            return int.Parse(User.FindFirst("userTypeId").Value);
        }
    }
}
