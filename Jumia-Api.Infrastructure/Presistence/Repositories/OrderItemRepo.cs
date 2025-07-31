using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Infrastructure.Presistence.Repositories
{
    public class OrderItemRepo : GenericRepo<OrderItem>, IOrderItemRepo
    {
        public OrderItemRepo(JumiaDbContext context) : base(context)
        {
        }

        public async Task<List<OrderItem>> GetBySellerId(int sellerId)
        {
            return await _dbSet.Where(oi => oi.SubOrder.SellerId == sellerId).ToListAsync();
                                                         

        }
    }
}
