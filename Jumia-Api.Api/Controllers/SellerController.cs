using System.Text.Json;
using Jumia_Api.Application.Dtos.AuthDtos;
using Jumia_Api.Application.Dtos.SellerDtos;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly ISellerService _sellerService;

        public SellerController(IOtpService otpService, IEmailService emailService, IUserService userService, IAuthService authService,ISellerService sellerService)
        {
            _otpService = otpService;
            _emailService = emailService;
            _userService = userService;
            _authService = authService;
            _sellerService = sellerService;
        }
        [HttpPost("SellerRegister")]
        [Consumes("multipart/form-data")]

        public async Task<IActionResult> Register([FromForm] CreateSellerDto dto)
        {
            var result = await _sellerService.RegisterAsync(dto);

            if (!result.Successed)
            {
                return BadRequest(new { result.Message });
            }
            SetJwtCookie(result.Token);

            var userInfo = new
            {
                result.UserId,
                result.Email,
                result.UserName,
                result.UserRole

            };

            var userInfoJson = JsonSerializer.Serialize(userInfo);
            Response.Cookies.Append("UserInfo", userInfoJson, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            // in the front-end logic, after the user registers, direct them to the update personal details page if isFirstTimeLogin is true.
            return Ok(new { result.Message, isFirstTimeLogin = true });

        }

        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddMinutes(60)
            };
            Response.Cookies.Append("JumiaAuthCookie", token, cookieOptions);
        }
    }
}
