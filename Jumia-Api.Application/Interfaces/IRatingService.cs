using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumia_Api.Application.Dtos.RatingDtos;

namespace Jumia_Api.Application.Interfaces
{
    public interface IRatingService
    {
        Task<IEnumerable<RatingInfoDto>> GetAllRatings();
        Task<IEnumerable<RatingInfoDto>> GetAllRatingForAdmin();
        Task<IEnumerable<RatingInfoDto>> GetRatingsByProductId(int productId);
        Task<RatingInfoDto?> GetRatingById(int id);
        Task AddRating(RatingCreateDto dto);
        Task UpdateRating(RatingUpdateDto dto);
        Task DeleteRating(int id);

        Task<bool> AcceptRating(int id);

        Task<bool> RejectRating(int id);
        Task<bool> HasCustomerPurchasedProductAsync(int customerId, int productId);
    }
}
