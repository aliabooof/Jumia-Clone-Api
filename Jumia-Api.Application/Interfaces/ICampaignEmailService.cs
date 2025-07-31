using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface ICampaignEmailService
    {
        /// <summary>
        /// Requests an AI-driven email campaign for a seller's top products.
        /// This method queues the request for background processing.
        /// </summary>
        Task<string> RequestEmailCampaignAsync(int sellerId);

        /// <summary>
        /// Requests a monthly performance report for a seller.
        /// This method queues the request for background processing.
        /// </summary>
        Task<string> RequestMonthlyReportAsync(int sellerId, DateTime monthYear);

        /// <summary>
        /// Processes pending campaign and report generation jobs from the queue.
        /// This method is called by the background worker.
        /// </summary>
        Task ProcessPendingJobsAsync(CancellationToken stoppingToken);
    }
}
