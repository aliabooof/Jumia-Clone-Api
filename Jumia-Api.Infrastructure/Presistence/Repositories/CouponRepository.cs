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
    public class CouponRepository : GenericRepo<Coupon>, ICouponRepo
    {
        public CouponRepository(JumiaDbContext context) : base(context)
        {
        }

        public async Task CreateAsync(Coupon coupon)
        {
            await _dbSet.AddAsync(coupon);
        }

        public async Task<IEnumerable<Coupon>> GetAllActiveAsync()
        {
            var now = DateTime.UtcNow;
            return await _dbSet.Where(c => c.IsActive==true && c.StartDate <= now && c.EndDate >= now)
                .ToListAsync();
        }

        public async Task<Coupon?> GetByCodeAsync(string code)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Code == code && c.IsActive);
        }

        public async Task<bool> IncrementUsageAsync(string code)
        {
            var coupon = await _dbSet.FirstOrDefaultAsync(c => c.Code == code);

            if (coupon == null) { return false; }

            if (coupon.UsageLimit.HasValue && coupon.UsageCount >= coupon.UsageLimit.Value)
            {
                return false;
            }
            coupon.UsageCount++;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
