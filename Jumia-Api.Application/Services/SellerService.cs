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

        public async Task<IEnumerable<SellerInfo>> GetAll()
        {
            var sellers = await _unitOfWork.SellerRepo.GetAllAsync();
            var sellerInfos = new List<SellerInfo>();

            foreach (var seller in sellers)
            {
                var products = await _unitOfWork.ProductRepo.GetAvailableProductsBySellerId(seller.SellerId);
                var productIds = products.Select(p => p.ProductId).ToList();

                var subOrders = await _unitOfWork.SubOrderRepo.GetSubOrdersBySellerIdAsync(seller.SellerId);
                var User = await _userService.GetUserByIdAsync(seller.UserId);
                if(User == null)
                {
                    return null;
                }

                // Optional debug log
                foreach (var so in subOrders)
                {
                    Console.WriteLine($"SubOrder Id: {so.SubOrderId}, SellerId: {so.SellerId}, OrderItems: {(so.OrderItems == null ? "null" : so.OrderItems.Count.ToString())}");
                }

                var orderItems = subOrders
                    .SelectMany(so => so.OrderItems ?? new List<OrderItem>())
                    .Where(oi => productIds.Contains(oi.ProductId))
                    .ToList();

                var totalProductsSold = orderItems.Sum(oi => oi.Quantity);
                var totalAmountSold = orderItems.Sum(oi => oi.Quantity * oi.PriceAtPurchase);

                sellerInfos.Add(new SellerInfo
                {
                    SellerId = seller.SellerId,
                    UserId = seller.UserId,
                    BusinessName = seller.BusinessName,
                    ImageUrl = seller.ImageUrl,
                    BusinessDescription = seller.BusinessDescription,
                    BusinessLogo = seller.BusinessLogo,
                    IsVerified = seller.IsVerified,
                    VerifiedAt = seller.VerifiedAt,
                    Rating = seller.Rating,
                    TotalProductsSold = totalProductsSold,
                    TotalAmountSold = totalAmountSold,
                    SellerName = $"{User.FirstName} {User.LastName}",
                    Email = User.Email
                });
            }

            return sellerInfos;
        }

        public async Task<IEnumerable<SellerInfo>> GetSellerById(int sellerId)
        {
            var seller = await _unitOfWork.SellerRepo.GetByIdAsync(sellerId);
            if (seller == null)
            {
                return null; // Seller not found
            }

            var User = await _userService.GetUserByIdAsync(seller.UserId);
            if (User == null)
            {
                return null; // User not found
            }
            var products = await _unitOfWork.ProductRepo.GetAvailableProductsBySellerId(sellerId);
            var productIds = products.Select(p => p.ProductId).ToList();

            var subOrders = await _unitOfWork.SubOrderRepo.GetSubOrdersBySellerIdAsync(sellerId);

            // Optional debug log
            foreach (var so in subOrders)
            {
                Console.WriteLine($"SubOrder Id: {so.SubOrderId}, SellerId: {so.SellerId}, OrderItems: {(so.OrderItems == null ? "null" : so.OrderItems.Count.ToString())}");
            }

            var orderItems = subOrders
                .SelectMany(so => so.OrderItems ?? new List<OrderItem>())
                .Where(oi => productIds.Contains(oi.ProductId))
                .ToList();

            var totalProductsSold = orderItems.Sum(oi => oi.Quantity);
            var totalAmountSold = orderItems.Sum(oi => oi.Quantity * oi.PriceAtPurchase);

            return new List<SellerInfo>
            {
                new SellerInfo
                {
                    SellerId = seller.SellerId,
                    UserId = seller.UserId,
                    BusinessName = seller.BusinessName,
                    ImageUrl = seller.ImageUrl,
                    BusinessDescription = seller.BusinessDescription,
                    BusinessLogo = seller.BusinessLogo,
                    IsVerified = seller.IsVerified,
                    VerifiedAt = seller.VerifiedAt,
                    Rating = seller.Rating,
                    TotalProductsSold = totalProductsSold,
                    TotalAmountSold = totalAmountSold,
                    SellerName = $"{User.FirstName} {User.LastName}",
                    Email = User.Email
                }
            };

        }

        public async Task<bool> IsVerified(int sellerId)
        {
           var seller =await _unitOfWork.SellerRepo.GetByIdAsync(sellerId);
            if (seller == null)
            {
                return false; // Seller not found
            }

            seller.IsVerified = "Authorized";

            //seller.IsVerified = !seller.IsVerified; // Assuming you want to mark the seller as verified
            seller.VerifiedAt = DateTime.UtcNow; // Set the verification date
            await _unitOfWork.SaveChangesAsync();
            return true; // Verification successful

        }

        public async Task<bool> ToggleBlock(int sellerId)
        {
            var seller = await _unitOfWork.SellerRepo.GetByIdAsync(sellerId);
            if (seller == null)
            {
                return false; // Seller not found
            }

            // Toggle the block status
            string newStatus = seller.IsVerified == "Blocked" ? "Authorized" : "Blocked";

            seller.IsVerified = newStatus; // Update the block status
            await _unitOfWork.SaveChangesAsync();
            return true; // Block status toggled successfully
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

            if (!_fileService.IsValidImage(dto.BusinessLogo))
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Invalid image file. Allowed formats: jpg, png, gif, etc. Max size: 10MB."
                };
            }

            var bussinessLogoUrl = await _fileService.SaveFileAsync(dto.BusinessLogo, "sellers/logos");
            var seller = new Seller
            {
                UserId = user.Id,
                BusinessName = dto.BusinessName, // You may add BusinessName to the DTO
                ImageUrl = imageUrl,
                BusinessDescription=dto.BusinessDescription,
                BusinessLogo=bussinessLogoUrl
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
                UserRole = "Seller",
                UserTypeId = seller.SellerId
            };
        }

    }
}
