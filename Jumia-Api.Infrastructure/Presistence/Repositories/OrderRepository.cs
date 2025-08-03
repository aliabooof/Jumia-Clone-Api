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
    public class OrderRepository : GenericRepo<Order>, IOrderRepository
    {
        public OrderRepository(JumiaDbContext context) : base(context)
        {
        }


        public async Task<IEnumerable<Order>> GetByCustomerIdAsync(int customerId)
        {
            return await _dbSet
                .Where(o => o.CustomerId == customerId).AsSplitQuery()
                .Include(o=>o.SubOrders)
                .ThenInclude(o=>o.OrderItems)
                .ThenInclude(oi=>oi.Product)
                .AsNoTracking()
                .ToListAsync();
        }

        public  async Task<Order> GetWithDetailsAsync(int id)
        {
            return await   _dbSet
                .Where(o => o.OrderId == id).AsSplitQuery()
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Coupon)
                .Include(o => o.SubOrders)
                .ThenInclude(so => so.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Order>> GetAllWithDetailsAsync()
        {
            return await _dbSet
                .Include(o => o.Customer)
                .Include(o => o.Address)
                .Include(o => o.Coupon)
                .Include(o => o.SubOrders)
                .ThenInclude(so => so.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsNoTracking()
                .ToListAsync();
        }
        public async Task<bool> CancelOrderAsync(int id, string cancellationReason = null)
        {
            var order = await _dbSet.FindAsync(id);
            if (order == null || order.Status == "cancelled")
                return false;

            order.Status = "cancelled";
            if(order.PaymentStatus=="paid" && order.Status == "cancelled")
            {
                order.PaymentStatus = "refunded";

            }
            order.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(order);
            return true;
        }

        public async Task<bool> UpdateOrderStatus(int orderid, string stauts)
        {
            var order = await _dbSet.Include(o=>o.SubOrders).FirstOrDefaultAsync(o => o.OrderId == orderid);
            if (order ==null)
            {
                return false;
            }
            order.Status = stauts.ToLower();
            foreach (var sub in order.SubOrders)
            {
                sub.Status = order.Status;
                sub.StatusUpdatedAt = DateTime.UtcNow;
            }
            var DeliverdStatus = "Delivered";
            if (stauts == DeliverdStatus.ToLower())
            {
                order.PaymentStatus = "paid";
                foreach(var sub in order.SubOrders)
                {
                    sub.Status = "delivered";
                    sub.StatusUpdatedAt = DateTime.UtcNow;
                }
            }
            else if (stauts == "Cancelled")
            {
                order.PaymentStatus = "refunded";
            }
                order.UpdatedAt = DateTime.UtcNow;
            _dbSet.Update(order);
            return true;
        }
    }
}
