using Jumia_Api.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Domain.Interfaces.Repositories
{
   public  interface ICampaignJobRequestRepo:IGenericRepo<CampaignJobRequest>
    {
        Task<CampaignJobRequest> GetFirstPendingJobToExecute(CancellationToken stoppingToken);
    }
}
