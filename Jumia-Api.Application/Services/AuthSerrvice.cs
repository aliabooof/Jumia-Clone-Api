using Jumia_Api.Api.Contracts.Results;
using Jumia_Api.Application.Dtos.AuthDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Jumia_Api.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IOtpService _otpService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AuthService(IUserService userService, IJwtService jwtService, IOtpService otpService, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork)
        {
            _userService = userService;
            _jwtService = jwtService;
            _otpService = otpService;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResult> LoginAsync(LoginDTO dto)
        {
            var user = await _userService.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "User not found"
                };
            }
            var passwordValid = await _userService.CheckPasswordAsync(user, dto.Password);
            if (!passwordValid)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Invalid password"
                };
            }


            var role = await _userService.GetUserRoleAsync(user);
            int userTypeId=0;
            if (role == "Customer")
            {
                 var customer =await _unitOfWork.CustomerRepo.GetCustomerByUserIdAsync(user.Id);
              
                userTypeId = customer.CustomerId;
            } 
                
            if(role == "Seller")
            {
                var seller = await _unitOfWork.SellerRepo.GetSellerByUserID(user.Id);
                userTypeId = seller.SellerId;
            }
            var token = await _jwtService.GenerateJwtTokenAsync(user, role, userTypeId);
             return new AuthResult
            {
                Successed = true,
                Token = token,
                Message = "Login successful",
                UserId = user.Id,
                Email = user.Email,
                UserName = user.FirstName + " " + user.LastName,
                UserRole = role,
                UserTypeId=userTypeId
               
                
            };


        }

        public async Task<AuthResult> RegisterAsync(PasswordSetupDto dto)
        {
            if(dto.Password != dto.ConfirmPassword)
            {
                return new AuthResult
                {
                    Successed = false,
                    Message = "Passwords do not match"
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
            // Assign default role to the user
            var user = await _userService.FindByEmailAsync(dto.Email);
            if (user == null)
            {

                return new AuthResult
                {
                    Successed = false,
                    Message = "User not found after creation"
                };

            }
           
            await _userService.AddUserToRoleAsync(user, "Customer");

            var customer = new Customer()
            {
                UserId=user.Id,
                //FullName = $"{dto.FirstName} {dto.LastName}",
                //Gender = dto.Gender,
                //Address = dto.Address,
                //BirthDate = dto.BirthDate

            };
            // Add customer to the database
            await _unitOfWork.CustomerRepo.AddAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            //Generate token after registeration to allow user to login immediately
            var token = await _jwtService.GenerateJwtTokenAsync(user, "Customer",customer.CustomerId);

            return new AuthResult
            {
                Successed = true,
                Token = token,
                Message = "User registered successfully",
                UserId = user.Id,
                Email = user.Email,
                UserName = user.FirstName + " " + user.LastName,
                UserRole = "Customer"

            };

        }




        public async Task<(bool Success, string Message)> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return (false, "Role name cannot be empty.");

            var roleExists = await _roleManager.RoleExistsAsync(roleName);
            if (roleExists)
                return (false, $"Role '{roleName}' already exists.");

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
                return (true, $"Role '{roleName}' created successfully.");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, $"Failed to create role: {errors}");
        }

       
    }
}
