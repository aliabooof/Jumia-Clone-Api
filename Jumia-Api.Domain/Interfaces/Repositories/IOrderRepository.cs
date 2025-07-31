using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IOrderRepository : IGenericRepo<Order>
    {
        Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId);
        Task<Order> GetWithDetailsAsync(int id);
        Task<IEnumerable<Order>> GetAllWithDetailsAsync();

        Task<bool> UpdateOrderStatus(int orderid, string stauts);
        Task<bool> CancelOrderAsync(int id, string cancellationReason = null);
        
    }
}
