using Microsoft.AspNetCore.Mvc;
using BuzaiManagementApi.Repositories;

namespace BuzaiManagementApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepository;

        public ProductsController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // アクセス先: GET /api/products/sqlserver-text/{id}
        [HttpGet("sqlserver-text/{id}")]
        public async Task<IActionResult> GetSqlServerText(string id)
        {
            try
            {
                var product = await _productRepository.GetByProductNoAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "該当するデータが見つかりません" });
                }

                return Ok(new { text_value = product.ProductName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "サーバーエラー", detail = ex.Message });
            }
        }
    }
}