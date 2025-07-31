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

namespace Jumia_Api.Application.Services
{
    public class CampaignEmailService : ICampaignEmailService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly OllamaApiClient _ollamaClient;
        private readonly IEmailService _emailSender; // Your email sending service
        private readonly ILogger<CampaignEmailService> _logger;
        private readonly JumiaDbContext _appContext;

        public CampaignEmailService(

            IConfiguration configuration,
            IEmailService emailSender,
            ILogger<CampaignEmailService> logger,
            IUnitOfWork unitOfWork,
            JumiaDbContext appContext)
        {

            _ollamaClient = new OllamaApiClient("http://localhost:11434");
            _emailSender = emailSender;
            _logger = logger;
            _unitOfWork = unitOfWork;
            _appContext = appContext;
            // QuestPDF.Settings.License = LicenseType.Community; // Uncomment if using Community License
        }

        // --- Methods for Requesting Jobs (Called by API or Scheduler) ---

        public async Task<string> RequestEmailCampaignAsync(int sellerId)
        {
            // Initial check for seller's total sales volume
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
                PayloadJson = JsonSerializer.Serialize(new { SellerId = sellerId }) // Store any specific data needed
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

        // --- Method for Processing Jobs (Called by Background Worker) ---

        public async Task ProcessPendingJobsAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Checking for pending campaign jobs...");

            // Select a single pending job to process to avoid overwhelming the system
            // Use a transaction and locking for concurrent workers if needed in high-volume scenarios
            var job = await _unitOfWork.CampaignJobRequestRepo.GetFirstPendingJobToExecute(stoppingToken);

            if (job == null)
            {
                _logger.LogInformation("No pending jobs found.");
                return;
            }

            _logger.LogInformation($"Processing Job ID: {job.Id}, Type: {job.JobType}, Seller: {job.SellerId}");

            job.Status = JobStatus.Processing;
            job.StartedProcessingAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(stoppingToken); // Update status immediately

            try
            {
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
            }
        }

        private async Task ProcessEmailCampaignJobAsync(CampaignJobRequest job, CancellationToken stoppingToken)
        {
            var sellerId = job.SellerId;
            var seller = job.Seller; // Already loaded via Include

            // 1. Identify Top 3 Selling Products for THIS Seller
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

            // 2. Generate Email Content (HTML) for each top product using Ollama
            var emailHtmlContent = new StringBuilder();
            emailHtmlContent.AppendLine($"<h1>Discover Hot Products from {seller.BusinessName}!</h1>");
            emailHtmlContent.AppendLine("<p>We've noticed you love great deals, and our top seller, " + seller.BusinessName + ", has some amazing products that are flying off the digital shelves!</p>");

             // Or your preferred Ollama model

            foreach (var product in topProductsForSeller)
            {
                stoppingToken.ThrowIfCancellationRequested(); // Check for cancellation
                var prompt = $"""
        Generate an engaging marketing email snippet in HTML for the following product.
        The snippet should be a self-contained HTML `<div>` or `<section>`.
        Focus on highlighting key benefits, encouraging a purchase, and making it visually appealing within an email.
        Use inline CSS for styling.

        Product Name: {product.Name}
        Product Description: {product.Description}
        Product Price: {product.BasePrice:C}
        Product URL: http://localhost:4200/Products/{product.ProductId}
        Product Image URL: http://localhost:5087{product.MainImageUrl}

        Ensure the snippet includes:
        - Product Name (h3)
        - Product Image (img tag with provided URL, responsive style)
        - Engaging description (p tag)
        - Price (strong or larger font)
        - A clear "Shop Now" button (a tag with inline styling) linking to Product URL.
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

                var productHtmlSnippet = new StringBuilder(); // StringBuilder for this specific product's snippet
                await foreach (var streamPart in streamedResponse.WithCancellation(stoppingToken))
                {
                    if (streamPart?.Message?.Content != null)
                    {
                        productHtmlSnippet.Append(streamPart.Message.Content);
                    }
                }
                var result = productHtmlSnippet.ToString().Trim();
                result = CleanOllamaResponse(result);

                // Now, append the *completed* snippet for this product to the main email content
                if (result.Length > 0)
                {
                    emailHtmlContent.AppendLine(result.ToString());
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


            // ... rest of the method (customer targeting and email sending) remains the same ...
            var targetedCustomerEmails = await _appContext.Customers
                                                       .Select(c => c.User.Email)
                                                       .Take(500) // Limit for testing, adjust as needed
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
            var seller = job.Seller; // Already loaded via Include

            // Parse month/year from payload
            var payload = JsonSerializer.Deserialize<MonthlyReportPayload>(job.PayloadJson ?? "{}");
            var month = payload?.Month ?? DateTime.UtcNow.AddMonths(-1).Month;
            var year = payload?.Year ?? DateTime.UtcNow.AddMonths(-1).Year;

            var reportMonthStart = new DateTime(year, month, 1);
            var reportMonthEnd = reportMonthStart.AddMonths(1).AddDays(-1);

            // 1. Gather Data for the Seller for the Report Month
            var sellerOrderItems = await _appContext.OrderItems
                                                 .Include(oi => oi.SubOrder)
                                                 .Include(oi => oi.Product)
                                                 .Where(oi => oi.SubOrder.SellerId == seller.SellerId &&
                                                              oi.SubOrder.Order.CreatedAt >= reportMonthStart &&
                                                              oi.SubOrder.Order.CreatedAt <= reportMonthEnd)
                                                 .ToListAsync(stoppingToken);

            var totalRevenue = sellerOrderItems.Sum(oi =>  oi.TotalPrice);
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

            var analysisTextBuilder = new StringBuilder(); // StringBuilder for the full analysis text
            await foreach (var streamPart in streamedResponse.WithCancellation(stoppingToken))
            {
                if (streamPart?.Message?.Content != null)
                {
                    analysisTextBuilder.Append(streamPart.Message.Content);
                }
            }

            var analysisText = analysisTextBuilder.ToString(); // Get the complete analysis text
            analysisText = CleanOllamaResponse(analysisText);
            QuestPDF.Settings.License = LicenseType.Community;

            // ... rest of the method (PDF generation and email sending) remains the same ...
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
                            column.Item().Text(analysisText) // Use the aggregated analysisText
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

        // Helper class for deserializing monthly report payload
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


            // If the report always starts with "### Executive Summary",
            // you could specifically look for that and remove anything before it,
            // but be careful not to remove valid content.
            var executiveSummaryIndex = cleanedText.IndexOf("### Executive Summary", StringComparison.OrdinalIgnoreCase);
            if (executiveSummaryIndex > 0)
            {
                cleanedText = cleanedText.Substring(executiveSummaryIndex);
            }

            return cleanedText;
        }
    }
}
