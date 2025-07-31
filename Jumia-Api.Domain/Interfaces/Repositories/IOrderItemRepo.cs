using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IOrderItemRepo : IGenericRepo<OrderItem>
    {
        Task<List<OrderItem>> GetBySellerId (int sellerId);
        
    }
}
