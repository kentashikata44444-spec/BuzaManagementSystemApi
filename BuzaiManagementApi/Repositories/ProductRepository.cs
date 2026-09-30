using System.Data;
using Dapper;
using BuzaiManagementApi.Models;

namespace BuzaiManagementApi.Repositories
{
    public interface IProductRepository
    {
        Task<Product?> GetByProductNoAsync(string productNo);
    }

    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public ProductRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Product?> GetByProductNoAsync(string productNo)
        {
            using var conn = _connectionFactory.CreateSqlServerConnection();
            
            string sql = "SELECT [製品No] AS ProductNo, [製品名] AS ProductName FROM dbo.[製品マスタ] WHERE [製品No] = @ProductNo";
            return await conn.QueryFirstOrDefaultAsync<Product>(sql, new { ProductNo = productNo });
        }
    }
}