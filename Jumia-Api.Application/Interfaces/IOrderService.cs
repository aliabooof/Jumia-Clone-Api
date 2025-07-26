using Jumia_Api.Application.Dtos.OrderDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDTO> CreateOrderAsync(CreateOrderDTO orderDto);
        Task<OrderDTO> GetOrderByIdAsync(int id);
        Task<IEnumerable<OrderDTO>> GetAllOrdersAsync();
        Task<IEnumerable<OrderDTO>> GetOrdersByCustomerIdAsync(int customerId);
        Task<OrderDTO> UpdateOrderAsync(int id, UpdateOrderDTO orderDto);
        Task<bool> DeleteOrderAsync(int id);
        Task<bool> CancelOrderAsync(int id, string cancellationReason = null);
        Task<IEnumerable<SubOrderDTO>> GetSubOrdersByOrderIdAsync(int orderId);
        Task<IEnumerable<SubOrderDTO>> GetSubOrdersBySellerIdAsync(int sellerId);
    }
}
