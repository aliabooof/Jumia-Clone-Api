using Jumia_Api.Application.Dtos.CartDto;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
      {
            int customerId = GetCustomerId();
            var cart = await _cartService.GetCartAsync(customerId);
            return Ok(cart);
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItems([FromBody] List<AddToCartDto> dtos)
        {
            int customerId = GetCustomerId();
            await _cartService.AddItemsAsync(customerId, dtos);
            return Ok();
        }

        [HttpPut("items/{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] UpdateCartItemDto dto)
        {
            int customerId = GetCustomerId();
            var result = await _cartService.UpdateItemQuantityAsync(customerId, id, dto.Quantity);
            return Ok(result);
        }

        [HttpDelete("items/{id}")]
        public async Task<IActionResult> RemoveItem(int id)
        {
            int customerId = GetCustomerId();
            await _cartService.RemoveItemAsync(customerId, id);
            return Ok();
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            int customerId = GetCustomerId();
            await _cartService.ClearCartAsync(customerId);
            return Ok();
        }

        private int GetCustomerId()
        {
            return int.Parse(User.FindFirst("userTypeId").Value);
        }
    }
}
