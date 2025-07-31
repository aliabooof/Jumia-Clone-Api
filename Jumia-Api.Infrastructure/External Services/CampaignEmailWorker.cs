using Jumia_Api.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jumia_Api.Infrastructure.External_Services
{
    public class CampaignEmailWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CampaignEmailWorker> _logger;

        public CampaignEmailWorker(IServiceProvider serviceProvider, ILogger<CampaignEmailWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CampaignEmailWorker started.");

            // The worker will continuously try to process jobs.
            // The delay ensures it doesn't hammer the DB if there are no jobs.
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignEmailService>();

                try
                {
                    // This method will now try to pick up and process ONE pending job at a time.
                    // If no jobs are pending, it will return quickly.
                    await campaignService.ProcessPendingJobsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("CampaignEmailWorker operation cancelled.");
                    break; // Exit loop if cancellation is requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during campaign job processing in CampaignEmailWorker.");
                    // Consider implementing a back-off strategy here if errors are frequent
                }

                // Delay before checking for the next job. Adjust this based on expected job volume and desired responsiveness.
                // For a low-volume setup, 1 minute is fine. For high volume, you might want a shorter delay or
                // use a real message queue that pushes notifications.
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Reduced to 30 seconds for quicker polling
            }

            _logger.LogInformation("CampaignEmailWorker stopped.");
        }
    }
}
