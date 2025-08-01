
﻿using System;
﻿using Jumia_Api.Domain.Interfaces.Repositories;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Jumia_Api.Infrastructure.Presistence.Repositories
{
    public class SellerRepo : GenericRepo<Seller>, ISellerRepo
    {
        public SellerRepo(JumiaDbContext dbContext) : base(dbContext)
        {
            // Initialize the repository with the database context
            // This is where you would typically set up your DbSet<Seller> if using EF Core
        }
        public Task AddAsync(Seller entity)
        {
            throw new NotImplementedException();
        }

        public Task Delete(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Seller>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public async Task<Seller?> GetByIdAsync(int id)
        {
            return await _dbSet.FirstOrDefaultAsync(s => s.SellerId == id);
        }

        public async void Update(Seller entity)
        {
            _dbSet.Update(entity);
        }

        public async Task<Seller> GetSellerByUserID(string userId)
        {
            return await _dbSet.Where(s => s.UserId == userId).FirstOrDefaultAsync();

        }
    }



}

