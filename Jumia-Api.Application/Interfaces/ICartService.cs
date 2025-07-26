using Jumia_Api.Application.Dtos.CartDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(int customerId);
        Task AddItemsAsync(int customerId, List<AddToCartDto> dtos );
        Task<CartItemDto> UpdateItemQuantityAsync(int customerId, int cartItemId, int quantity);
        Task RemoveItemAsync(int customerId, int cartItemId);
        Task ClearCartAsync(int customerId);
    }
}
