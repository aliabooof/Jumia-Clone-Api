using StackExchange.Redis;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Jumia_Api.Infrastructure.Redis; // Add this using directive

namespace Jumia_Api.Infrastructure.External_Services // Assuming this is the correct namespace
{
    public class RedisKeyExpiryService : BackgroundService
    {
        private readonly ILogger<RedisKeyExpiryService> _logger;
        private readonly IConnectionMultiplexer _redisConnection;
        private readonly ISubscriber _redisSubscriber;
        private readonly IServiceScopeFactory _serviceScopeFactory; // <--- CHANGE HERE

        public RedisKeyExpiryService(
            ILogger<RedisKeyExpiryService> logger,
            IConnectionMultiplexer redisConnection,
            IServiceScopeFactory serviceScopeFactory) // <--- CHANGE HERE
        {
            _logger = logger;
            _redisConnection = redisConnection;
            _redisSubscriber = _redisConnection.GetSubscriber();
            _serviceScopeFactory = serviceScopeFactory; // <--- CHANGE HERE
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RedisKeyExpiryService running.");

            // Configure Redis to emit keyspace notifications for expired events
            try
            {
                var server = _redisConnection.GetServer(_redisConnection.GetEndPoints().First());
                var config = await server.ConfigGetAsync("notify-keyspace-events");
                var currentEvents = config.FirstOrDefault(x => x.Key == "notify-keyspace-events").Value;

                if (!currentEvents.Contains("Ex"))
                {
                    await server.ConfigSetAsync("notify-keyspace-events", currentEvents + "Ex");
                    _logger.LogInformation("Enabled Redis keyspace notifications for expired events (Ex).");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to configure Redis keyspace notifications. Ensure 'notify-keyspace-events' is enabled in redis.conf with 'Ex'.");
            }


            // Initial setup for the monthly report trigger
            // Create a scope for the initial call
            using (var scope = _serviceScopeFactory.CreateScope()) // <--- NEW SCOPE
            {
                var reportKeyHandler = scope.ServiceProvider.GetRequiredService<ReportKeyHandler>(); // Resolve inside scope
                await reportKeyHandler.ConfigureMonthlyReportTriggerAsync();
            }


            // Subscribe to expired key events
            await _redisSubscriber.SubscribeAsync("__keyevent@0__:expired", async (channel, key) =>
            {
                _logger.LogInformation($"Redis key expired: {key}");
                // Create a new scope for each expired key event processing
                using (var scope = _serviceScopeFactory.CreateScope()) // <--- NEW SCOPE FOR EACH EVENT
                {
                    var reportKeyHandler = scope.ServiceProvider.GetRequiredService<ReportKeyHandler>(); // Resolve inside scope
                    await reportKeyHandler.HandleExpiredKeyAsync(key!);
                }
            });

            _logger.LogInformation("RedisKeyExpiryService subscribed to expired key events.");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}