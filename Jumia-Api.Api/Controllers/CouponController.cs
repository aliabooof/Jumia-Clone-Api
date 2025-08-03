using Jumia_Api.Application.Dtos.CouponDtos;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CouponController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly IUserCouponService _userCouponService;

        public CouponController(ICouponService couponService, IUserCouponService userCouponService)
        {
            _couponService = couponService;
            _userCouponService = userCouponService;
        }

        [HttpGet("GetAllCoupons")]
        public async Task<IActionResult> GetAllActiveCoupons()
        {
            var coupons = await _couponService.GettAllActiveCouponAsync();
            if (coupons == null || !coupons.Any())
            {
                return NotFound("No active coupons found.");
            }
            return Ok(coupons);
        }

        [HttpGet("GetCouponByCode/{code}")]
        public async Task<IActionResult> GetCouponByCode(string code)
        {
            var coupon = await _couponService.GetCouponByCodeAsync(code);
            if (coupon == null)
            {
                return NotFound($"Coupon with code {code} not found.");
            }
            return Ok(coupon);
        }

        [HttpPost("CreateCoupon")]
        [Produces("application/json")]

        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCoupon( CreateCouponDto dto)
        {
            var result = await _couponService.CreateCouponAsync(dto);
            if (!result)
            {
                return BadRequest("Failed to create coupon. Please check the input data.");
            }
            return Ok("Coupon created successfully.");
        }

        [HttpPut("UpdateCoupon/{couponId}")]
        [Produces("application/json")]

        //[Authorize]
        public async Task<IActionResult> UpdateCoupon(int couponId, CreateCouponDto dto)
        {
            var result = await _couponService.UpdateCouponAsync(couponId, dto);
            if (!result)
            {
                return BadRequest("Failed to update coupon. Please check the input data.");
            }
            return Ok("Coupon updated successfully.");
        }

        [HttpDelete("DeleteCoupon/{couponId}")]
        [Produces("application/json")]

        //[Authorize]
        public async Task<IActionResult> DeleteCoupon(int couponId)
        {
            var result = await _couponService.DeleteCouponAsync(couponId);
            if (!result)
            {
                return BadRequest("Failed to delete coupon. Please check the coupon ID.");
            }
            return Ok("Coupon deleted successfully.");
        }
        [Produces("application/json")]
        [HttpPost("ApplyCoupon/{code}")]
        public async Task<IActionResult> ApplyCoupon( ApplyCouponDto dto)
        {
            var result = await _couponService.ApplyCouponAsync(dto.Code, dto.CartTotal);
            if (!result)
            {
                return BadRequest("Failed to apply coupon. Please check the coupon code and cart total.");
            }
            return Ok("Coupon applied successfully.");
        }

        [HttpPost("AssignCoupon")]
        //[Authorize(Roles ="Admin")]
        public async Task<IActionResult> AssignCouponToUser([FromBody] UserCouponActionDto dto)
        {
            var result = await _userCouponService.AssignCouponToUserAsync(dto.UserId, dto.CouponId);
            if (!result)
            {
                return BadRequest("Failed to assign coupon to user. Please check the user ID and coupon ID.");
            }
            return Ok("Coupon assigned to user successfully.");
        }

        [HttpPost("MarkCouponAsUsed")]
        //[Authorize]
        public async Task<IActionResult> MarkCouponAsUsed([FromBody] UserCouponActionDto dto)
        {
            var result = await _userCouponService.MarkCouponAsUsedAsync(dto.UserId, dto.CouponId);
            if (!result)
            {
                return BadRequest("Failed to mark coupon as used. Please check the user ID and coupon ID.");
            }
            return Ok("Coupon marked as used successfully.");
        }

        [HttpGet("UserCoupons/{userId}")]
        //[Authorize]
        public async Task<IActionResult> GetUserCoupons(int userId)
        {
            var userCoupons = await _userCouponService.GetUserCouponsAsync(userId);
            if (userCoupons == null || !userCoupons.Any())
            {
                return NotFound($"No coupons found for user with ID {userId}.");
            }
            return Ok(userCoupons);
        }

        [HttpDelete("DeleteUserCoupon/{userCouponId}")]
        //[Authorize(Roles ="Admin")]
        public async Task<IActionResult> DeleteUserCoupon(int userCouponId)
        {
            var result = await _userCouponService.DeleteUserCouponAsync(userCouponId);
            if (!result)
            {
                return BadRequest("Failed to delete user coupon. Please check the user coupon ID.");
            }
            return Ok("User coupon deleted successfully.");
        }






    }
}
