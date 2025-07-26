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
    public class SellerRepo : GenericRepo<Seller>, ISellerRepo
    {
        public SellerRepo(JumiaDbContext context) : base(context)
        {
        }

        public async Task<Seller> GetSellerByUserID(string userId)
        {
            return await _dbSet.Where(s=>s.UserId ==userId).FirstOrDefaultAsync();
        }
    }
}
