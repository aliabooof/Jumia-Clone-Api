using Jumia_Api.Application.Dtos.RatingDtos;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpGet("GetAllRatings")]
        public async Task<ActionResult<IEnumerable<RatingInfoDto>>> GetAllRatings()
        {
            var ratings = await _ratingService.GetAllRatings();
            return Ok(ratings);
        }

        [HttpGet("GetAllRatingsForAdmin")]
        public async Task<ActionResult<IEnumerable<RatingInfoDto>>> GetAllForAdmin()
        {
            var ratings = await _ratingService.GetAllRatingForAdmin();
            return Ok(ratings);
        }


        [HttpGet("hasPurchased")]
        public async Task<IActionResult> HasPurchased([FromQuery] int customerId, [FromQuery] int productId)
        {
            var hasBought = await _ratingService.HasCustomerPurchasedProductAsync(customerId, productId);
            return Ok(hasBought);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRatingById(int id)
        {
            var rating = await _ratingService.GetRatingById(id);
            return rating is null ? NotFound(new { Message = "Rating not found." }) : Ok(rating);
        }

        [HttpGet("ByProduct/{productId}")]
        public async Task<IActionResult> GetRatingsByProductId(int productId)
        {
            var ratings = await _ratingService.GetRatingsByProductId(productId);
            return Ok(ratings);
        }

        [HttpPost]
        public async Task<IActionResult> AddRating(RatingCreateDto dto)
        {
            try
            {
                await _ratingService.AddRating(dto);
                return Ok(new { Message = "Rating added successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while adding the rating.Or you Must Buy this Product First", Details = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRating(RatingUpdateDto dto)
        {
            try
            {
                await _ratingService.UpdateRating(dto);
                return Ok(new { Message = "Rating updated successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the rating.", Details = ex.Message });
            }
        }

        [HttpPut("Accept/{ratingID}")]
        public async Task<IActionResult> AcceptRating(int ratingID)
        {
            try
            {
                var result = await _ratingService.AcceptRating(ratingID);
                return result ? Ok(new { Message = "Rating accepted successfully." }) : BadRequest(new { Message = "Failed to accept rating." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while accepting the rating.", Details = ex.Message });
            }
        }

        [HttpPut("Reject/{ratingID}")]
        public async Task<IActionResult> RejectRating(int ratingID)
        {
            try
            {
                var result = await _ratingService.RejectRating(ratingID);
                return result ? Ok(new { Message = "Rating rejected successfully." }) : BadRequest(new { Message = "Failed to reject rating." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while rejecting the rating.", Details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRating(int id)
        {
            try
            {
                await _ratingService.DeleteRating(id);
                return Ok(new { Message = "Rating deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while deleting the rating.", Details = ex.Message });
            }
        }
    }
}
