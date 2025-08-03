using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Enums;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Jumia_Api.Infrastructure.Presistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http; // For OllamaApiClient constructor
using StackExchange.Redis; // <--- ADD THIS USING

namespace Jumia_Api.Application.Services
{
    public class CampaignEmailService : ICampaignEmailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly OllamaApiClient _ollamaClient;
        private readonly IEmailService _emailSender;
        private readonly ILogger<CampaignEmailService> _logger;
        private readonly JumiaDbContext _appContext;
        private readonly IDatabase _redisDb; // <--- ADD THIS FIELD for Redis interaction

        public CampaignEmailService(
            IConfiguration configuration,
            IEmailService emailSender,
            ILogger<CampaignEmailService> logger,
            IUnitOfWork unitOfWork,
            JumiaDbContext appContext,
            IConnectionMultiplexer redisConnection) // <--- INJECT IConnectionMultiplexer
        {
            _ollamaClient = new OllamaApiClient("http://localhost:11434"); // Consider injecting OllamaApiClient if possible
            _emailSender = emailSender;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _appContext = appContext;
            _redisDb = redisConnection.GetDatabase(); // <--- GET REDIS DATABASE INSTANCE
            // QuestPDF.Settings.License = LicenseType.Community; // Uncomment if using Community License
        }

        // --- Methods for Requesting Jobs (No Changes Here) ---

        public async Task<string> RequestEmailCampaignAsync(int sellerId)
        {
            var ItemsSoldBySeller = await _unitOfWork.OrderItemRepo.GetBySellerId(sellerId);
            var totalItemsSoldBySeller = ItemsSoldBySeller.Sum(oi => oi.Quantity);

            if (totalItemsSoldBySeller < 5)
            {
                return $"Seller (ID: {sellerId}) has only sold {totalItemsSoldBySeller} items in total. 1000 items minimum required to launch a campaign.";
            }

            var jobRequest = new CampaignJobRequest
            {
                JobType = JobType.EmailCampaign,
                SellerId = sellerId,
                Status = JobStatus.Pending,
                PayloadJson = JsonSerializer.Serialize(new { SellerId = sellerId })
            };

            await _unitOfWork.CampaignJobRequestRepo.AddAsync(jobRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Email campaign request queued for seller {sellerId}. Job ID: {jobRequest.Id}");
            return $"Email campaign request submitted successfully. You will be notified when it's live. Job ID: {jobRequest.Id}";
        }

        public async Task<string> RequestMonthlyReportAsync(int sellerId, DateTime monthYear)
        {
            var jobRequest = new CampaignJobRequest
            {
                JobType = JobType.MonthlyReport,
                SellerId = sellerId,
                Status = JobStatus.Pending,
                PayloadJson = JsonSerializer.Serialize(new { SellerId = sellerId, Month = monthYear.Month, Year = monthYear.Year })
            };

            await _unitOfWork.CampaignJobRequestRepo.AddAsync(jobRequest);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Monthly report request queued for seller {sellerId} for {monthYear:yyyy-MM}. Job ID: {jobRequest.Id}");
            return $"Monthly report request submitted successfully. Job ID: {jobRequest.Id}";
        }

        // --- Method for Processing Jobs (Updated with Redis Leasing) ---

        public async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking for pending campaign jobs with Redis leasing...");

            // Select a single pending job. Redis will handle concurrent access.
            var job = await _appContext.CampaignJobRequests // Use DbContext directly for simpler query
                .Include(j => j.Seller) // Include Seller for easy access later
                .ThenInclude(s=>s.User)
                .Where(j => j.Status == JobStatus.Pending)
                .OrderBy(j => j.CreatedAt) // Process oldest first
                .FirstOrDefaultAsync(stoppingToken);

            if (job == null)
            {
                _logger.LogInformation("No pending jobs found.");
                return;
            }

            // --- Implement Redis Job Lease ---
            var jobLeaseKey = $"report:job:{job.Id}";
            // Set a lease duration (e.g., 10 minutes) - this should be longer than your max expected job processing time.
            var leaseExpiry = TimeSpan.FromMinutes(10);

            // Try to acquire a lease using SET NX (SET if Not eXists).
            // If the key is successfully set, this worker has the lease.
            bool acquiredLease = await _redisDb.StringSetAsync(jobLeaseKey, "processing", leaseExpiry, When.NotExists);

            if (!acquiredLease)
            {
                _logger.LogInformation($"Job {job.Id} is already being processed by another worker or lease still exists. Skipping.");
                return; // Another worker got the lease, or the previous worker crashed and the key hasn't expired yet.
            }

            _logger.LogInformation($"Acquired Redis lease for job {job.Id}. Processing...");

            try
            {
                // Update job status in SQL DB immediately after acquiring lease
                job.Status = JobStatus.Processing;
                job.StartedProcessingAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(stoppingToken); // Use SaveChangesAsync from UnitOfWork

                switch (job.JobType)
                {
                    case JobType.EmailCampaign:
                        await ProcessEmailCampaignJobAsync(job, stoppingToken);
                        break;
                    case JobType.MonthlyReport:
                        await ProcessMonthlyReportJobAsync(job, stoppingToken);
                        break;
                    default:
                        job.Status = JobStatus.Failed;
                        job.ErrorMessage = $"Unknown JobType: {job.JobType}";
                        _logger.LogError(job.ErrorMessage);
                        break;
                }

                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation($"Job {job.Id} completed successfully.");
            }
            catch (OperationCanceledException)
            {
                job.Status = JobStatus.Cancelled;
                job.ErrorMessage = "Operation cancelled.";
                _logger.LogWarning($"Job {job.Id} was cancelled.");
                throw; // Re-throw to propagate cancellation up to the worker
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message + (ex.InnerException != null ? " | Inner: " + ex.InnerException.Message : "");
                _logger.LogError(ex, $"Error processing job {job.Id}: {ex.Message}");
            }
            finally
            {
                await _unitOfWork.SaveChangesAsync(stoppingToken); // Save final status

                // Release the Redis lease if the job completed successfully or was cancelled.
                // If the job failed, we *let the key expire naturally* so that the ReportKeyHandler
                // can detect the failure/abandonment and perform its cleanup/logging.
                if (job.Status == JobStatus.Completed || job.Status == JobStatus.Cancelled)
                {
                    await _redisDb.KeyDeleteAsync(jobLeaseKey);
                    _logger.LogInformation($"Released Redis lease for job {job.Id}.");
                }
                else
                {
                    _logger.LogWarning($"Job {job.Id} finished with status '{job.Status}'. Leaving Redis key '{jobLeaseKey}' to expire naturally for potential cleanup.");
                }
            }
        }

        // --- Your Existing Private Processing Methods (No Changes, only added cancellation token checks) ---

        private async Task ProcessEmailCampaignJobAsync(CampaignJobRequest job, CancellationToken stoppingToken)
        {
            var sellerId = job.SellerId;
            var seller = job.Seller;

            var topProductsForSeller = await _appContext.OrderItems
                                    .Where(oi => oi.SubOrder.SellerId == sellerId)
                                    .GroupBy(oi => oi.ProductId)
                                    .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(oi => oi.Quantity) })
                                    .OrderByDescending(x => x.TotalQuantity)
                                    .Take(3)
                                    .Join(_appContext.Products,
                                            oi => oi.ProductId,
                                            p => p.ProductId,
                                            (oi, p) => p)
                                    .ToListAsync(stoppingToken);

            if (!topProductsForSeller.Any())
            {
                throw new InvalidOperationException($"No top products found for seller (ID: {sellerId}) to generate campaign.");
            }

            var emailHtmlContent = new StringBuilder();
            emailHtmlContent.AppendLine($"<h1>Discover Hot Products from {seller.BusinessName}!</h1>");
            emailHtmlContent.AppendLine("<p>We've noticed you love great deals, and our top seller, " + seller.BusinessName + ", has some amazing products that are flying off the digital shelves!</p>");

            foreach (var product in topProductsForSeller)
            {
                stoppingToken.ThrowIfCancellationRequested(); // Check for cancellation
                var fullImageUrl = @"http://localhost:5087" + product.MainImageUrl;
                var prompt = $"""
        Your task is to generate **EXACTLY ONE HTML SNIPPET** for an email marketing campaign.
        This snippet must be a **self-contained HTML <div> or <section> element**.

        **STRICT RULES:**
        1.  **ONLY generate the HTML code.** Do NOT include any other text, explanations, conversational filler, or Markdown code blocks (e.g., ```html, ```css, ```).
        2.  Do NOT include any internal thoughts, planning, or commentary.
        3.  The HTML must be valid and ready to be inserted directly into an email body.
        4.  Use inline CSS for styling within the HTML tags where possible, as external CSS is not supported by all email clients.

        **Product Details to Use:**
        Product Name: {product.Name}
        Product Description: {product.Description}
        Product Price: {product.BasePrice:C}
        Product URL: http://localhost:4200/Products/{product.ProductId}
        Product Image URL: {fullImageUrl}

        **Ensure the HTML snippet includes:**
        -   Product Name (within an `<h3>` tag).
        -   Product Image (an `<img>` tag with `src="{fullImageUrl}"` and appropriate responsive styling like `max-width: 100%; height: auto;`).
        -   An engaging description (within a `<p>` tag).
        -   Price (formatted strongly or with larger font, e.g., `<p><strong>Price: {product.BasePrice:C}</strong></p>`).
        -   A clear "Shop Now" button (an `<a>` tag with inline styling for button appearance, linking to `Product URL`).


        EXAMPLE START OF DESIRED OUTPUT:
        <div style="font-family: Arial, sans-serif; text-align: center; padding: 20px; border: 1px solid #ddd; border-radius: 8px; margin-bottom: 20px;">
            <h3>...</h3>
            <img src="..." alt="..." style="max-width: 100%; height: auto;">
            <p>...</p>
            <p><strong>...</strong></p>
            <a href="..." style="background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;">Shop Now</a>
        </div>
        END EXAMPLE.

        Your response should start directly with the opening `<div>` or `<section>` tag of the product snippet.
        """;

                var messages = new List<Message>
                {
                    new Message(ChatRole.System, "You are a highly skilled e-commerce marketing assistant. Your task is to generate compelling, clean HTML email snippets for products."),
                    new Message(ChatRole.User, prompt)
                };

                var chatRequest = new ChatRequest()
                {
                    Model = "qwen3:0.6b",
                    Messages = messages,
                    Stream = true
                };

                var streamedResponse = _ollamaClient.ChatAsync(chatRequest, stoppingToken);

                var productHtmlSnippet = new StringBuilder();
                await foreach (var streamPart in streamedResponse.WithCancellation(stoppingToken))
                {
                    if (streamPart?.Message?.Content != null)
                    {
                        productHtmlSnippet.Append(streamPart.Message.Content);
                    }
                }
                var result = productHtmlSnippet.ToString().Trim();
                result = CleanOllamaResponse(result);

                if (result.Length > 0)
                {
                    emailHtmlContent.AppendLine(result); // Append result directly
                }
                else
                {
                    _logger.LogWarning($"Ollama did not return content for product {product.Name} (ID: {product.ProductId}).");
                    emailHtmlContent.AppendLine($"<p>Error generating content for {product.Name}.</p>");
                }
            }

            var fullHtmlEmail = $"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>Hot Products from {seller.BusinessName}!</title>
</head>
<body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0;">

    <div style="max-width: 600px; margin: 20px auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);">

        {emailHtmlContent.ToString()}

        <p style="text-align: center; margin-top: 30px; font-size: 0.9em; color: #777;">
            Thanks for being a valued customer! <br>
            <a href="http://localhost:4200/products-brand/{seller.SellerId}" style="color: #007bff; text-decoration: none;">Visit {seller.BusinessName}'s Store</a>
        </p>

    </div>

</body>
</html>
""";

            var targetedCustomerEmails = await _appContext.Customers
                                        .Select(c => c.User.Email)
                                        .Take(500)
                                        .ToListAsync(stoppingToken);

            if (!targetedCustomerEmails.Any())
            {
                throw new InvalidOperationException("No customers found to send the campaign to.");
            }

            await _emailSender.SendBulkEmailAsync(
                targetedCustomerEmails,
                $"🚀 Hot Picks from {seller.BusinessName} - Don't Miss Out!",
                fullHtmlEmail
            );

            _logger.LogInformation($"Successfully processed email campaign job {job.Id} for seller {sellerId}.");
        }

        private async Task ProcessMonthlyReportJobAsync(CampaignJobRequest job, CancellationToken stoppingToken)
        {
            var sellerId = job.SellerId;
            var seller = job.Seller;

            var payload = JsonSerializer.Deserialize<MonthlyReportPayload>(job.PayloadJson ?? "{}");
            var month = payload?.Month ?? DateTime.UtcNow.AddMonths(-1).Month;
            var year = payload?.Year ?? DateTime.UtcNow.AddMonths(-1).Year;

            var reportMonthStart = new DateTime(year, month, 1);
            var reportMonthEnd = reportMonthStart.AddMonths(1).AddDays(-1);

            var sellerOrderItems = await _appContext.OrderItems
                                        .Include(oi => oi.SubOrder)
                                        .Include(oi => oi.Product)
                                        .Where(oi => oi.SubOrder.SellerId == seller.SellerId &&
                                                        oi.SubOrder.Order.CreatedAt >= reportMonthStart &&
                                                        oi.SubOrder.Order.CreatedAt <= reportMonthEnd)
                                        .ToListAsync(stoppingToken);

            var totalRevenue = sellerOrderItems.Sum(oi => oi.TotalPrice);
            var totalItemsSold = sellerOrderItems.Sum(oi => oi.Quantity);

            var topProducts = sellerOrderItems
                                .GroupBy(oi => oi.Product.Name)
                                .Select(g => new { ProductName = g.Key, Quantity = g.Sum(oi => oi.Quantity) })
                                .OrderByDescending(x => x.Quantity)
                                .Take(5)
                                .ToList();

            var dataSummary = new StringBuilder();
            dataSummary.AppendLine($"Seller Name: {seller.BusinessName}");
            dataSummary.AppendLine($"Reporting Period: {reportMonthStart:yyyy-MM-dd} to {reportMonthEnd:yyyy-MM-dd}");
            dataSummary.AppendLine($"Total Revenue: {totalRevenue:C}");
            dataSummary.AppendLine($"Total Items Sold: {totalItemsSold}");
            dataSummary.AppendLine("Top Products (by quantity sold):");
            foreach (var p in topProducts)
            {
                dataSummary.AppendLine($"- {p.ProductName}: {p.Quantity} units");
            }

            var prompt = $"""
        Analyze the following monthly sales and performance data for an e-commerce seller.
        Generate a concise executive summary, key highlights, and actionable recommendations to help the seller grow their business.
        The output should be clear, professional, and formatted with distinct headings.

        Seller Data for the last month:
        {dataSummary.ToString()}

        Structure your response with the following sections:
        ### Executive Summary
        ### Key Performance Highlights
        ### Top Products Spotlight
        ### Recommendations for Growth
        """;

            var messages = new List<Message>
            {
                new Message(ChatRole.System, "You are an expert e-commerce business analyst. Provide insights and actionable advice based on seller performance data."),
                new Message(ChatRole.User, prompt)
            };

            var chatRequest = new ChatRequest()
            {
                Model = "qwen3:0.6b",
                Messages = messages,
                Stream = true
            };

            var streamedResponse = _ollamaClient.ChatAsync(chatRequest, stoppingToken);

            var analysisTextBuilder = new StringBuilder();
            await foreach (var streamPart in streamedResponse.WithCancellation(stoppingToken))
            {
                if (streamPart?.Message?.Content != null)
                {
                    analysisTextBuilder.Append(streamPart.Message.Content);
                }
            }

            var analysisText = analysisTextBuilder.ToString();
            analysisText = CleanOllamaResponse(analysisText);
            QuestPDF.Settings.License = LicenseType.Community;

            byte[] pdfBytes;
            pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("Jumia E-commerce Platform").FontSize(10).SemiBold().AlignRight();
                            column.Item().Text($"Monthly Performance Report for {seller.BusinessName}")
                                .FontSize(28).Bold().FontColor(Colors.Blue.Darken4)
                                .ParagraphSpacing(10);
                            column.Item().Text($"Period: {reportMonthStart:MMMM dd, yyyy} - {reportMonthEnd:MMMM dd, yyyy}")
                                .FontSize(12).FontColor(Colors.Grey.Darken2);
                        });

                    page.Content()
                        .PaddingVertical(20)
                        .Column(column =>
                        {
                            column.Spacing(15);
                            column.Item().Text(analysisText)
                                .FontSize(11)
                                .LineHeight(1.5f);

                            column.Item().PaddingTop(20).Text("Raw Data Summary").FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
                            column.Item().Column(innerColumn =>
                            {
                                innerColumn.Item().Text($"• Total Revenue: {totalRevenue:C}").FontSize(11);
                                innerColumn.Item().Text($"• Total Items Sold: {totalItemsSold}").FontSize(11);
                                innerColumn.Item().Text("• Top Products:").FontSize(11);
                                innerColumn.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(1).Padding(5).Text("Product Name").Bold();
                                        header.Cell().BorderBottom(1).Padding(5).Text("Units Sold").Bold();
                                    });

                                    foreach (var p in topProducts)
                                    {
                                        table.Cell().BorderBottom(0.5f).Padding(5).Text(p.ProductName);
                                        table.Cell().BorderBottom(0.5f).Padding(5).Text(p.Quantity.ToString());
                                    }
                                });
                            });
                        });

                    page.Footer()
                        .AlignRight()
                        .Text(x =>
                        {
                            x.Span("Page ").FontSize(10);
                            x.CurrentPageNumber().FontSize(10);
                            x.Span(" of ").FontSize(10);
                            x.TotalPages().FontSize(10);
                        });
                });
            }).GeneratePdf();

            await _emailSender.SendEmailWithAttachmentAsync(
                seller.User.Email,
                $"Your Monthly Performance Report for {reportMonthStart:MMMM yyyy}",
                $"Dear {seller.BusinessName},\n\nPlease find attached your monthly performance report for {reportMonthStart:MMMM yyyy}.\n\nBest regards,\nJumia Team",
                $"Jumia_Report_{seller.BusinessName.Replace(" ", "_")}_{reportMonthStart:yyyyMM}.pdf",
                pdfBytes
            );

            _logger.LogInformation($"Successfully processed monthly report job {job.Id} for seller {sellerId}.");
        }

        private class MonthlyReportPayload
        {
            public int SellerId { get; set; }
            public int Month { get; set; }
            public int Year { get; set; }
        }

        private string CleanOllamaResponse(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return string.Empty;
            }
            var cleanedText = rawText.Trim();

            cleanedText = Regex.Replace(cleanedText, "<think>(.*?)</think>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var htmlMatch = Regex.Match(cleanedText, "```html(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (htmlMatch.Success && htmlMatch.Groups.Count > 1)
            {
                return htmlMatch.Groups[1].Value.Trim();
            }

            var fallbackMatch = Regex.Match(cleanedText, @"<(div|section)>(.*?)</\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (fallbackMatch.Success && fallbackMatch.Groups.Count > 1)
            {
                return fallbackMatch.Value.Trim();
            }
            // For the monthly report, if it's markdown, we remove fences only, not the content
            var markdownMatch = Regex.Match(cleanedText, "```markdown(.*?)```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (markdownMatch.Success && markdownMatch.Groups.Count > 1)
            {
                return markdownMatch.Groups[1].Value.Trim();
            }

            var executiveSummaryIndex = cleanedText.IndexOf("### Executive Summary", StringComparison.OrdinalIgnoreCase);
            if (executiveSummaryIndex > 0)
            {
                cleanedText = cleanedText.Substring(executiveSummaryIndex);
            }

            return cleanedText;
        }
    }
}