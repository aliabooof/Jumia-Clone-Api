using Jumia_Api.Application.Dtos.AiDtos;
using Jumia_Api.Application.Interfaces;
using Jumia_Api.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiQueryController : ControllerBase
    {
        private readonly IProductAiService _aiService;
        private readonly IProductService _productService;

        public AiQueryController(IProductAiService aiService, IProductService productService)
        {
            _aiService = aiService;
            _productService = productService;
        }

        //        [HttpPost("search")]
        //        public async Task<IActionResult> Search([FromBody] AiQueryRequestDto request)
        //        {
        //            var filters = await _aiService.ParseQueryToFilterAsync(request.Query);
        //            var products = await _productService.GetFilteredProductsPagedAsync(filters, request.Page, request.PageSize);

        //            return Ok(new AiQueryResponseDto
        //            {
        //                OriginalQuery = request.Query,
        //                Filters = filters,
        //                Products = products
        //            });
        //        }

        //        [HttpGet("similar/{productId}")]
        //        public async Task<IActionResult> GetSimilar(int productId, int page = 1, int pageSize = 20)
        //        {
        //            var filters = await _aiService.GetSimilarProductsFilterAsync(productId);
        //            var products = await _productService.GetFilteredProductsPagedAsync(filters, page, pageSize);

        //            return Ok(new AiQueryResponseDto
        //            {
        //                OriginalQuery = $"Find similar to Product #{productId}",
        //                Filters = filters,
        //                Products = products
        //            });
        //        }
        //}

        [HttpPost("ask")]
        public async Task<IActionResult> AskQuestion([FromBody] AiQuestionRequestDto request)
        {
            var answer = await _aiService.AnswerProductQuestionAsync(request.Question, request.ProductId);
            return Ok(new AiQuestionResponseDto
            {
                Question = request.Question,
                ProductId = request.ProductId,
                Answer = answer
            });
        }

        [HttpPost("ai/index-products")]
        public async Task<IActionResult> IndexProducts()
        {
            await _aiService.IndexAllProductsAsync();
            return Ok("Products indexed in Qdrant.");
        }

        [HttpGet("semantic-search")]
        public async Task<IActionResult> SemanticSearch(
        [FromQuery] string query,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            var result = await _aiService.SemanticSearch(query, pageNumber, pageSize);

            if (result == null || !result.Any())
                return NotFound("No products found matching the query.");

            return Ok(result);
        }

    }
}
