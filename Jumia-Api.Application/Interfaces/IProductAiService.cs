using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Get;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumia_Api.Application.Interfaces
{
    public interface IProductAiService
    {
        Task<ProductAttributeDto> ParseQueryToFilterAsync(string query);
        Task<ProductAttributeDto> GetSimilarProductsFilterAsync(int productId);
        Task<string> AnswerProductQuestionAsync(string question, int? productId = null);
        Task IndexAllProductsAsync();
        Task<IEnumerable<ProductsUIDto>> SemanticSearch(string query, int pageNumber = 1, int pageSize = 20);
    }
}
