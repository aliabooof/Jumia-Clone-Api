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
    public class SuborderRepo : GenericRepo<SubOrder>,IsuborderRepo
    {
        public SuborderRepo(JumiaDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<SubOrder>> GetSubOrdersByOrderIdAsync(int orderId)
        {
            return await _dbSet
                .AsSplitQuery()
                .Where(so => so.OrderId == orderId)
                .Include(so => so.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<SubOrder>> GetSubOrdersBySellerIdAsync(int sellerId)
        {
            return await _dbSet
                .AsSplitQuery()
                .Where(so => so.SellerId == sellerId)
                .Include(so => so.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
