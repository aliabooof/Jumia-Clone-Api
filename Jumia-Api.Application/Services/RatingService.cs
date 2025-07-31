using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jumia_Api.Application.Dtos.RatingDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Domain.Interfaces.UnitOfWork;
using Jumia_Api.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Jumia_Api.Application.Services
{
    public class RatingService : IRatingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RatingService> _logger;
        private readonly UserManager<AppUser> _userManager;

        public RatingService(IUnitOfWork unitOfWork, ILogger<RatingService> logger,UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task AddRating(RatingCreateDto dto)
        {
            var orders = await _unitOfWork.OrderRepo.GetByCustomerIdAsync(dto.CustomerId);

            bool hasBought = orders
                .Where(o => string.Equals(o.Status, "delivered", StringComparison.OrdinalIgnoreCase))
                .SelectMany(o => o.SubOrders)
                .SelectMany(sub => sub.OrderItems)
                .Any(item => item.ProductId == dto.ProductId);

            if (!hasBought)
                throw new InvalidOperationException("You can only rate a product you have purchased.");

            var rating = new Rating
            {
                CustomerId = dto.CustomerId,
                ProductId = dto.ProductId,
                Stars = dto.Stars,
                Comment = dto.Comment,
                IsVerifiedPurchase = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RatingRepo.AddAsync(rating);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteRating(int id)
        {
            var rating = await _unitOfWork.RatingRepo.GetByIdAsync(id);
            if (rating == null)
                throw new KeyNotFoundException("Rating not found.");

            await _unitOfWork.RatingRepo.Delete(rating.RatingId);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<RatingInfoDto>> GetAllRatings()
        {
            var ratings = await _unitOfWork.RatingRepo.GetAllAsync();
            var verifiedRatings = ratings.Where(r =>
    string.Equals(r.IsVerified, "verified", StringComparison.OrdinalIgnoreCase));
            var ratingDtos = new List<RatingInfoDto>();


            foreach (var rating in verifiedRatings)
            {
                string customerName = "Unknown";
                var productname = await _unitOfWork.ProductRepo.GetByIdAsync(rating.ProductId);
                var customer = await _unitOfWork.CustomerRepo.GetByIdAsync(rating.CustomerId);
                if (customer != null)
                {
                    var user = await _userManager.FindByIdAsync(customer.UserId);
                    if (user != null)
                    {
                        customerName = $"{user.FirstName} {user.LastName}";
                    }
                }

                ratingDtos.Add(new RatingInfoDto
                {
                    RatingId = rating.RatingId,
                    CustomerId = rating.CustomerId,
                    CustomerName = customerName,
                    ProductName=productname.Name,
                    Stars = rating.Stars,
                    Comment = rating.Comment,
                    CreatedAt = rating.CreatedAt,
                    IsVerifiedPurchase = rating.IsVerifiedPurchase,
                    IsAccepted=rating.IsVerified,
                    HelpfulCount = rating.HelpfulCount
                });
            }

            return ratingDtos;
        }


        public async Task<bool> AcceptRating(int ratingid)
        {
            var rating = await _unitOfWork.RatingRepo.GetByIdAsync(ratingid);
            if (rating == null)
                throw new KeyNotFoundException("Rating not found.");

            rating.IsVerified = "verified";
            _unitOfWork.RatingRepo.Update(rating);
            await _unitOfWork.SaveChangesAsync();

            return true;

        }

        public async Task<bool> RejectRating(int ratingid)
        {
            var rating = await _unitOfWork.RatingRepo.GetByIdAsync(ratingid);
            if (rating == null)
                throw new KeyNotFoundException("Rating not found.");
            rating.IsVerified = "rejected";
            _unitOfWork.RatingRepo.Update(rating);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }


        public async Task<RatingInfoDto?> GetRatingById(int id)
        {
            var rating = await _unitOfWork.RatingRepo.GetByIdAsync(id);
            if (rating == null)
                return null;

            var customer = await _unitOfWork.CustomerRepo.GetByIdAsync(rating.CustomerId);
            var product= await _unitOfWork.ProductRepo.GetByIdAsync(rating.ProductId);

            // Try to load the User manually if it's not loaded automatically
            var user = customer != null
                ? await _userManager.FindByIdAsync(customer.UserId)
                : null;


            return new RatingInfoDto
            {
                RatingId = rating.RatingId,
                CustomerId = rating.CustomerId,
                CustomerName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : "Unknown",
                ProductName = product.Name,
                Stars = rating.Stars,
                Comment = rating.Comment,
                CreatedAt = rating.CreatedAt,
                IsVerifiedPurchase = rating.IsVerifiedPurchase,
                HelpfulCount = rating.HelpfulCount
            };
        }


        public async Task<IEnumerable<RatingInfoDto>> GetRatingsByProductId(int productId)
        {
            var ratings = await _unitOfWork.RatingRepo.GetAllAsync();
            var filteredRatings = ratings.Where(r => r.ProductId == productId);
            var verifiedRatings = filteredRatings
                .Where(r => string.Equals(r.IsVerified, "verified", StringComparison.OrdinalIgnoreCase));
            var ratingDtos = new List<RatingInfoDto>();

            foreach (var rating in verifiedRatings)
            {
                string customerName = "Unknown";
                var productname = await _unitOfWork.ProductRepo.GetByIdAsync(rating.ProductId);

                var customer = await _unitOfWork.CustomerRepo.GetByIdAsync(rating.CustomerId);
                if (customer != null)
                {
                    var user = await _userManager.FindByIdAsync(customer.UserId);
                    if (user != null)
                    {
                        customerName = $"{user.FirstName} {user.LastName}";
                    }
                }

                ratingDtos.Add(new RatingInfoDto
                {
                    RatingId = rating.RatingId,
                    CustomerId = rating.CustomerId,
                    CustomerName = customerName,
                    ProductName = productname.Name,
                    Stars = rating.Stars,
                    Comment = rating.Comment,
                    CreatedAt = rating.CreatedAt,
                    IsVerifiedPurchase = rating.IsVerifiedPurchase,
                    HelpfulCount = rating.HelpfulCount
                });
            }

            return ratingDtos;
        }

        public async Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId)
        {
            var orders = await _unitOfWork.OrderRepo.GetByCustomerIdAsync(customerId);
            bool hasBought = orders
                .Where(o => string.Equals(o.Status, "delivered", StringComparison.OrdinalIgnoreCase))
                .SelectMany(o => o.SubOrders)
                .SelectMany(sub => sub.OrderItems)
                .Any(item => item.ProductId == productId);

            return hasBought;
        }


        public async Task UpdateRating(RatingUpdateDto dto)
        {
            var rating = await _unitOfWork.RatingRepo.GetByIdAsync(dto.RatingId);
            if (rating == null)
                throw new KeyNotFoundException("Rating not found.");

            rating.Stars = dto.Stars;
            rating.Comment = dto.Comment;
            rating.CreatedAt = DateTime.UtcNow;

            _unitOfWork.RatingRepo.Update(rating);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<RatingInfoDto>> GetAllRatingForAdmin()
        {

            var ratings = await _unitOfWork.RatingRepo.GetAllAsync();
            var ratingDtos = new List<RatingInfoDto>();


            foreach (var rating in ratings)
            {
                string customerName = "Unknown";
                var productname = await _unitOfWork.ProductRepo.GetByIdAsync(rating.ProductId);
                var customer = await _unitOfWork.CustomerRepo.GetByIdAsync(rating.CustomerId);
                if (customer != null)
                {
                    var user = await _userManager.FindByIdAsync(customer.UserId);
                    if (user != null)
                    {
                        customerName = $"{user.FirstName} {user.LastName}";
                    }
                }

                ratingDtos.Add(new RatingInfoDto
                {
                    RatingId = rating.RatingId,
                    CustomerId = rating.CustomerId,
                    CustomerName = customerName,
                    ProductName = productname.Name,
                    Stars = rating.Stars,
                    Comment = rating.Comment,
                    CreatedAt = rating.CreatedAt,
                    IsVerifiedPurchase = rating.IsVerifiedPurchase,
                    IsAccepted= rating.IsVerified,
                    HelpfulCount = rating.HelpfulCount
                });
            }

            return ratingDtos;
        }
    }
}
