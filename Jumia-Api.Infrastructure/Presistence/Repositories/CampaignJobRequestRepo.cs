using Jumia_Api.Domain.Enums;
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
    public class CampaignJobRequestRepo : GenericRepo<CampaignJobRequest>, ICampaignJobRequestRepo
    {
        public CampaignJobRequestRepo(JumiaDbContext context) : base(context)
        {
        }

        public async Task<CampaignJobRequest> GetFirstPendingJobToExecute(CancellationToken stoppingToken)
        {
            return await _dbSet.Where(j => j.Status == JobStatus.Pending)
                                      .OrderBy(j => j.CreatedAt) 
                                      .Include(j=>j.Seller)
                                      .ThenInclude(s=>s.User)
                                      .FirstOrDefaultAsync(stoppingToken);

        }
    }
}
