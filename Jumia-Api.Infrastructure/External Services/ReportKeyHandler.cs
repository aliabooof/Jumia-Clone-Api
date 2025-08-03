using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Jumia_Api.Domain.Models;
using Jumia_Api.Domain.Enums;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using Jumia_Api.Application.Interfaces;

namespace Jumia_Api.Infrastructure.Redis
{
    public class ReportKeyHandler
    {
        private readonly ILogger<ReportKeyHandler> _logger;
        private readonly IDatabase _redisDb;
        private readonly IServiceProvider _serviceProvider;

        public ReportKeyHandler(ILogger<ReportKeyHandler> logger,
                                IConnectionMultiplexer redis,
                                IServiceProvider serviceProvider)
        {
            _logger = logger;
            _redisDb = redis.GetDatabase();
            _serviceProvider = serviceProvider;
        }

        public async Task ConfigureMonthlyReportTriggerAsync()
        {
            const string triggerKey = "monthly:report:campaign:trigger";

            System.DateTime utcNow = System.DateTime.UtcNow;

            System.DateTime endOfCurrentUtcMonth = new System.DateTime(utcNow.Year, utcNow.Month, 1, 23, 59, 59, System.DateTimeKind.Utc)
                                            .AddMonths(1)
                                            .AddDays(-1);

            //System.TimeSpan expiryDuration = endOfCurrentUtcMonth.Subtract(utcNow);
            System.TimeSpan expiryDuration = System.TimeSpan.FromMinutes(2);

            //if (expiryDuration <= System.TimeSpan.Zero)
            //{
            //    _logger.LogWarning($"Monthly report trigger setup: Already past end of current UTC month ({endOfCurrentUtcMonth:yyyy-MM-dd HH:mm:ss}). Setting for end of next UTC month.");
            //    endOfCurrentUtcMonth = new System.DateTime(utcNow.Year, utcNow.Month, 1, 23, 59, 59, System.DateTimeKind.Utc)
            //                            .AddMonths(2)
            //                            .AddDays(-1);
            //    expiryDuration = endOfCurrentUtcMonth.Subtract(utcNow);
            //}

            StackExchange.Redis.RedisValueWithExpiry existingEntry = await _redisDb.StringGetWithExpiryAsync(triggerKey);

            bool keyHasValue = !existingEntry.Value.IsNullOrEmpty;
            bool keyHasExpiry = existingEntry.Expiry.HasValue;

            bool expiryIsInFuture = false;
            System.DateTime calculatedExistingExpiryUtc = System.DateTime.MinValue; // This will be our absolute DateTime for comparison

            if (keyHasExpiry)
            {
                // !!! THIS IS THE CRUCIAL CHANGE BASED ON YOUR ERROR MESSAGE !!!
                // If Expiry.Value is TimeSpan, it's the remaining TTL.
                // Add it to UtcNow to get the estimated absolute expiry time (DateTime).
                System.TimeSpan remainingTtl = (System.TimeSpan)existingEntry.Expiry.Value; // Cast to TimeSpan as per error
                calculatedExistingExpiryUtc = utcNow.Add(remainingTtl); // Add TimeSpan to DateTime to get new DateTime

                expiryIsInFuture = calculatedExistingExpiryUtc > utcNow; // DateTime > DateTime
            }

            bool expiryIsAligned = false;
            if (keyHasExpiry)
            {
                // Now compare our calculated absolute expiry (DateTime) with our target (DateTime)
                System.TimeSpan alignmentDifference = calculatedExistingExpiryUtc.Subtract(endOfCurrentUtcMonth).Duration();
                expiryIsAligned = alignmentDifference < System.TimeSpan.FromDays(2);
            }

            //bool keyExistsAndIsCorrectlySet = keyHasValue && keyHasExpiry && expiryIsInFuture && expiryIsAligned;
            bool keyExistsAndIsCorrectlySet = false;

            if (!keyExistsAndIsCorrectlySet)
            {
                string triggerValue = endOfCurrentUtcMonth.ToString("yyyy-MM");
                await _redisDb.StringSetAsync(triggerKey, triggerValue, expiryDuration);
                _logger.LogInformation($"Configured Redis monthly report trigger '{triggerKey}' to expire at {endOfCurrentUtcMonth.ToLocalTime():yyyy-MM-dd HH:mm:ss} Local Time ({expiryDuration.TotalMinutes:F0} minutes from now).");
            }
            else
            {
                _logger.LogInformation($"Redis monthly report trigger '{triggerKey}' is already correctly configured to expire on {calculatedExistingExpiryUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss} Local Time.");
            }
        }

        public async Task HandleExpiredKeyAsync(string expiredKey)
        {
            if (expiredKey.StartsWith("report:job:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = expiredKey.Split(':');
                if (parts.Length >= 3)
                {
                    var jobIdString = parts[2];
                    if (long.TryParse(jobIdString, out long jobId))
                    {
                        _logger.LogWarning($"Redis key '{expiredKey}' expired. This typically indicates an abandoned or failed job.");

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<JumiaDbContext>();
                            var jobRequest = await dbContext.CampaignJobRequests.FindAsync(jobId);

                            if (jobRequest != null)
                            {
                                if (jobRequest.Status == JobStatus.Pending || jobRequest.Status == JobStatus.Processing)
                                {
                                    jobRequest.Status = JobStatus.Failed;
                                    jobRequest.ErrorMessage = "Job expired in Redis (timeout or abandoned).";
                                    jobRequest.CompletedAt = DateTime.UtcNow;
                                    await dbContext.SaveChangesAsync();
                                    _logger.LogError($"Marked CampaignJobRequest {jobId} as FAILED due to Redis key expiry. Previous status: {jobRequest.Status}.");
                                }
                                else
                                {
                                    _logger.LogInformation($"Report job {jobId} expired, but its status is already '{jobRequest.Status}'. No further action needed by expiry handler.");
                                }
                            }
                            else
                            {
                                _logger.LogWarning($"Expired Redis key '{expiredKey}' for CampaignJobRequest ID '{jobId}' not found in database.");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError($"Invalid job ID format in expired Redis key: {expiredKey}");
                    }
                }
            }
            else if (expiredKey.Equals("monthly:report:campaign:trigger", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Redis key '{expiredKey}' expired. Initiating monthly report campaign creation.");

                System.DateTime reportForMonthUtc = System.DateTime.UtcNow.AddMonths(-1);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<JumiaDbContext>();
                    var campaignEmailService = scope.ServiceProvider.GetRequiredService<ICampaignEmailService>();

                    var allSellers = await dbContext.Sellers
                                                    .Select(s => s.SellerId)
                                                    .ToListAsync();

                    if (allSellers.Any())
                    {
                        _logger.LogInformation($"Found {allSellers.Count} sellers. Requesting monthly reports for {reportForMonthUtc.ToLocalTime():MMMM yyyy} (Local Time).");
                        foreach (var sellerId in allSellers)
                        {
                            try
                            {
                                await campaignEmailService.RequestMonthlyReportAsync(sellerId, reportForMonthUtc);
                                _logger.LogInformation($"Requested monthly report for seller {sellerId} for {reportForMonthUtc:yyyy-MM}.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to request monthly report for seller {sellerId} for {reportForMonthUtc:yyyy-MM}.");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No sellers found to generate monthly reports for.");
                    }
                }

                await ConfigureMonthlyReportTriggerAsync();
            }
            else
            {
                _logger.LogDebug($"Expired key: {expiredKey} - no specific handler found for this key prefix.");
            }
        }
    }
}