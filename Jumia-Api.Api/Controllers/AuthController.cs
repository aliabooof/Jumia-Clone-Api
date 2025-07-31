using Jumia_Api.Application.Dtos.AuthDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IConfirmationEmailService _confirmationEmailService;

        public AuthController(IOtpService otpService, IUserService userService, IAuthService authService, IConfirmationEmailService confirmationEmailService)
        {
            _otpService = otpService;

            _userService = userService;
            _authService = authService;
            _confirmationEmailService = confirmationEmailService;
        }

        [HttpPost("email-check")]
        public async Task<IActionResult> CheckEmail([FromBody]EmailCheckDto dto)
        {
            if(await _userService.UserExistsAsync(dto.Email))
            {
                return Ok(new {isRegistered = true, message = "Email already registered"});
            }
            var otp = _otpService.GenerateOtp(dto.Email);
           _confirmationEmailService.SendConfirmationEmailAsync(dto.Email, otp, "otpcode");

            return Ok(new {isRegistered = false, message = "Email not registered, OTP sent", otp });
        }

        [HttpPost("verify-otp")]
        public IActionResult VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            var isValid = _otpService.ValidateOtp(dto.Email, dto.OtpCode);

            if (!isValid)
            {
                return BadRequest(new { message = "Invalid or expired OTP code" });
            }
            return Ok(new { otpValid = true });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PasswordSetupDto dto)
        {
            var result = await _authService.RegisterAsync(dto);

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
                result.UserRole,
                result.UserTypeId,
                result.isVerified
                
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
            return Ok(new { result.Message , isFirstTimeLogin = true});
           
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var result = await _authService.LoginAsync(dto);

            if (!result.Successed)

            {
                return Unauthorized(new { result.Message });
            }

            SetJwtCookie(result.Token);

            var userInfo = new
            {
                result.UserId,
                result.Email,
                result.UserName,
                result.UserRole,
                result.UserTypeId,
                result.isVerified,
                result.SellerStatus
            };

            var userInfoJson = JsonSerializer.Serialize(userInfo);
            Response.Cookies.Append("UserInfo", userInfoJson, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new { result.Message, result.Token });

        }

        [HttpDelete("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("JumiaAuthCookie");
            Response.Cookies.Delete("UserInfo");
            return Ok(new { message = "Logged out successfully" });

        }


        private void SetJwtCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("JumiaAuthCookie", token, cookieOptions);
        }

        //extract the user ID from the token
        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        [HttpPost]
        public async Task<IActionResult> CreateRole([FromQuery]string role)
        {
            var (success, message) = await _authService.CreateRoleAsync(role);

            if (success)
                return Ok(message);

            return BadRequest(message);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgetPasswordDto dto)
        {
            var result = await _authService.ForgetPasswordAsync(dto.Email);

            if (!result.Successed)
            {
                return BadRequest(new 
                { 
                    result.Message
                });
            }

            return Ok(new 
            {   result.Successed,
                result.Message,
                result.Token
               
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);

            if (!result.Successed)
            {
                return BadRequest(new { result.Message });
            }

            return Ok(new { result.Message });
        }




    }
}

