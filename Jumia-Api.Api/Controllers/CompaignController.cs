using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CampaignController : ControllerBase
    {
        private readonly ICampaignEmailService _campaignEmailService;
        private readonly ILogger<CampaignController> _logger;

        public CampaignController(ICampaignEmailService campaignEmailService, ILogger<CampaignController> logger)
        {
            _campaignEmailService = campaignEmailService;
            _logger = logger;
        }

        /// <summary>
        /// Requests an AI-driven email campaign for a specific seller.
        /// The request is queued for background processing.
        /// </summary>
        /// <param name="sellerId">The ID of the seller for whom to generate the campaign.</param>
        /// <returns>A status message indicating if the campaign was queued successfully or declined.</returns>
        [HttpPost("request-email/{sellerId}")] // Example: POST /api/campaigns/request-email/123
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestEmailCampaign(int sellerId)
        {
            if (sellerId <= 0)
            {
                _logger.LogWarning("Invalid Seller ID received for campaign request: {SellerId}", sellerId);
                return BadRequest("Invalid Seller ID. Must be a positive integer.");
            }

            try
            {
                // Call the service method, which now just queues the job
                string resultMessage = await _campaignEmailService.RequestEmailCampaignAsync(sellerId);

                // Based on the string result, determine the HTTP response
                if (resultMessage.Contains("minimum required to launch a campaign", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Campaign request declined for seller {SellerId}: {Message}", sellerId, resultMessage);
                    return BadRequest(resultMessage); // Business logic decline
                }
                else if (resultMessage.Contains("queued successfully", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Email campaign request successfully queued for seller {SellerId}: {Message}", sellerId, resultMessage);
                    return Ok(new { message = resultMessage, status = "queued" });
                }
                else
                {
                    // Catch any unexpected messages from the service
                    _logger.LogError("Unexpected response from campaign service for seller {SellerId}: {Message}", sellerId, resultMessage);
                    return StatusCode(500, "An unexpected issue occurred while processing your request.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled error occurred while requesting an email campaign for seller {SellerId}.", sellerId);
                return StatusCode(500, "An internal server error occurred. Please try again later.");
            }
        }

        // You could also add an endpoint for manually requesting a monthly report if needed for testing/admin
        [HttpPost("request-monthly-report/{sellerId}/{year}/{month}")]
        public async Task<IActionResult> RequestMonthlyReport(int sellerId, int year, int month)
        {
            if (sellerId <= 0) return BadRequest("Invalid Seller ID.");
            if (month < 1 || month > 12 || year < 2020 || year > DateTime.UtcNow.Year + 1) return BadRequest("Invalid month or year.");

            try
            {
                var reportDate = new DateTime(year, month, 1);
                string resultMessage = await _campaignEmailService.RequestMonthlyReportAsync(sellerId, reportDate);
                _logger.LogInformation("Monthly report request for seller {SellerId}, {Year}-{Month}: {Message}", sellerId, year, month, resultMessage);
                return Ok(new { message = resultMessage, status = "queued" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting monthly report for seller {SellerId}, {Year}-{Month}", sellerId, year, month);
                return StatusCode(500, "An error occurred while processing your report request.");
            }
        }
    }
}
