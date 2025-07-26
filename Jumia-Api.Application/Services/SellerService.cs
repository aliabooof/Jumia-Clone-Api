using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumia_Api.Api.Contracts.Results;
using Jumia_Api.Application.Dtos.AuthDtos;
using Jumia_Api.Application.Dtos.SellerDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Jumia_Api.Application.Services
{
    public class SellerService : ISellerService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IFileService _fileService;

        public SellerService(IUserService userService, IJwtService jwtService, IOtpService otpService, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork,IFileService fileService)
        {
            _userService = userService;
            _jwtService = jwtService;
            _otpService = otpService;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _fileService = fileService;
        }

        public async Task<AuthResult> RegisterAsync(CreateSellerDto dto)
        {
            if (dto.ConfirmPassword != dto.Password)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Password and Confirm Password do not match"
                };
            }

            var otpValid = _otpService.ValidateOtp(dto.Email, dto.OtpCode);
            if (!otpValid)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Invalid or expired OTP code"
                };
            }

            var result = await _userService.CreateUserAsync(
                dto.Email, dto.Password, dto.FirstName, dto.LastName, dto.BirthDate, dto.Gender, dto.Address
            );

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                return new AuthResult
                {
                    Successed = false,
                    Message = $"User creation failed: {errors}"
                };
            }

            _otpService.RemoveOtp(dto.Email);

            var user = await _userService.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "User not found after creation"
                };
            }

            await _userService.AddUserToRoleAsync(user, "Seller");

            // Handle image upload here (simplified):
            if (!_fileService.IsValidImage(dto.Image))
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Invalid image file. Allowed formats: jpg, png, gif, etc. Max size: 10MB."
                };
            }

            var imageUrl = await _fileService.SaveFileAsync(dto.Image, "sellers");


            var seller = new Seller
            {
                UserId = user.Id,
                BusinessName = $"{dto.FirstName} {dto.LastName}", // You may add BusinessName to the DTO
                ImageUrl = imageUrl,
                BusinessDescription=dto.BusinessDescription,
                BusinessLogo=dto.BusinessLogo
            };

            await _unitOfWork.Repository<Seller>().AddAsync(seller);
            await _unitOfWork.SaveChangesAsync();

            var token = await _jwtService.GenerateJwtTokenAsync(user, "Seller", seller.SellerId);

            return new AuthResult
            {
                Successed = true,
                Token = token,
                Message = "Seller registered successfully",
                UserId = user.Id,
                Email = user.Email,
                UserName = $"{user.FirstName} {user.LastName}",
                UserRole = "Seller"
            };
        }

    }
}
