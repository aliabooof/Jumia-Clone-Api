using Jumia_Api.Application.Dtos.ProductDtos;
using Jumia_Api.Application.Dtos.ProductDtos.Post;
using Jumia_Api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Jumia_Api.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        public readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }






        [HttpGet("get-details-by-id")]
        public async Task<IActionResult> GetProductByID([FromQuery] int productId, [FromQuery] string role)
        {
            var product = await _productService.GetProductDetailsAsync(productId,role);

            if (product == null)
            {
                return NotFound(new { message = "Product not found" });
            }

            return Ok(product);

        }

        [HttpGet("get-all-with-details")]
        public async Task<IActionResult> GetAllProducts()
        {
           var products = await _productService.GetAllProductsWithDetailsAsync();
            if (products == null || !products.Any())
            {
                return NotFound(new { message = "No products found" });
            }
            return Ok(products);
        }

        [HttpGet("get-all")]
        public async Task<IActionResult> GetAllAvailableProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            if (products == null || !products.Any())
            {
                return NotFound(new { message = "No available products found" });
            }

            return Ok(products);
        }

        [HttpGet("all-by-sellerId")]
        public async Task<IActionResult> GetAllBySellerIdAvailableProducts([FromQuery] int sellerId,[FromQuery]string role)
        {
            var products = await _productService.GetProductsBySellerIdAsync(sellerId,role);
            if (products == null || !products.Any())
            {
                return NotFound(new { message = "No available products found" });
            }

            return Ok(products);
        }



        [HttpPost("create")]
        
        public async Task<IActionResult> CreateProduct([FromForm] AddProductDto product)
        { //Done only return type checks
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
           var reuslt = await _productService.CreateProductAsync(product);
            return Ok(reuslt);
        }

        [HttpPost("Products-filterd")]
        public async Task<IActionResult> GetFilteredProducts([FromQuery] string role
                                                            ,[FromQuery] int pageNumber
                                                            , [FromQuery] int pageSize
                                                            ,[FromBody] ProductFilterRequestDto productFilterRequestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var products = await _productService.GetProductsByCategoriesAsync(role,productFilterRequestDto,pageNumber,pageSize);
            if (products == null ||products.Items ==null ||!products.Items.Any())
            {
                return NotFound(new { message = "No products found matching the criteria" });
            }
            return Ok(products);
        }

        [HttpPut("Deactivate/{id:int}")]
        public async Task<IActionResult> ActivateProduct(int id)
        {
            await _productService.DeactivateProductAsync(id);
            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpPut("Activate/{id:int}")]
        public async Task<IActionResult> DeactivateProduct(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _productService.ActivateProductAsync(id);
            return Ok(new { message = "Product updated successfully" });
        }

        [HttpGet("search")]
        // for user only
        public async Task<IActionResult> SearchProducts([FromQuery] string keyword)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
             var products = await _productService.SearchProductsAsync(keyword);
            return Ok(products);
        }

        
       
        [HttpPost("{id}/variant")]
        public async Task<IActionResult> FindVariant(int id, [FromBody] FindVariantRequestDto request)
        {
            var variant = await _productService.FindVariantAsync(id, request);
            return Ok(variant);
        }
        [HttpPost("{id}/attribute-options")]
        public async Task<IActionResult> GetAttributeOptions(int id, [FromBody] AttributeOptionsRequestDto request)
        {
            var options = await _productService.GetAttributeOptionsAsync(id, request);
            return Ok(options);
        }



        [HttpPut("update")]
        public async Task<IActionResult> UpdateProduct([FromForm] UpdateProductDto product)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            await _productService.UpdateProductAsync(product);
            return Ok();
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateProductStatus([FromQuery] int productId, [FromQuery] string status)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            await _productService.UpdateProductStatus(productId,status.ToLower());
            return Ok();
        }





    }
}
