using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
    public interface IsuborderRepo
    {
        Task<IEnumerable<SubOrder>> GetSubOrdersByOrderIdAsync(int orderId);
        Task<IEnumerable<SubOrder>> GetSubOrdersBySellerIdAsync(int sellerId);

    }
}
