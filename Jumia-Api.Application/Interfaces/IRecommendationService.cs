using Jumia_Api.Application.Dtos.ProductDtos.Get;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<IEnumerable<ProductDetailsDto>> GetRecommendationsAsync(int userId);

    }
}
