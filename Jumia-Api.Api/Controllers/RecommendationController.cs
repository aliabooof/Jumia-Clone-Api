using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("user-recommendations")]
        public async Task<IActionResult> GetUserRecommendations()
        {
            try
            {
                var customerId = GetCustomerId();
                var recommendations = await _recommendationService.GetRecommendationsAsync(customerId);
                return Ok(recommendations);
            }
            catch
            {
                return StatusCode(500, "Internal server error");
            }
        }

        private int GetCustomerId()
        {
            return int.Parse(User.FindFirst("userTypeId").Value);
        }
    }
}
