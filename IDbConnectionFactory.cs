using System;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace BuzaiManagementApi.Repositories
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateSqlServerConnection();
        IDbConnection CreatePostgresConnection();
    }

    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly ILogger<DbConnectionFactory> _logger;
        private readonly string _sqlServerConnStr;
        private readonly string _postgresConnStr;

        // ★ Program.cs から 3つの引数を受け取るコンストラクター
        public DbConnectionFactory(
            ILogger<DbConnectionFactory> logger, 
            string sqlServerConnStr, 
            string postgresConnStr)
        {
            _logger = logger;
            _sqlServerConnStr = sqlServerConnStr;
            _postgresConnStr = postgresConnStr;
        }

        public IDbConnection CreateSqlServerConnection()
        {
            return new SqlConnection(_sqlServerConnStr);
        }

        public IDbConnection CreatePostgresConnection()
        {
            return new NpgsqlConnection(_postgresConnStr);
        }
    }

    public static class DbConnectionFactoryExtensions
    {
        public static T ExecuteInPostgresTransaction<T>(
            this IDbConnectionFactory factory, 
            Func<IDbConnection, IDbTransaction, T> action,
            ILogger? logger = null)
        {
            logger?.LogInformation("PostgreSQL トランザクション処理を開始します。");

            using var conn = factory.CreatePostgresConnection();
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                T result = action(conn, transaction);
                transaction.Commit();
                logger?.LogInformation("PostgreSQL トランザクションを正常にコミットしました。");
                return result;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger?.LogError(ex, "PostgreSQL トランザクション内でエラーが発生したため、ロールバックしました: {Message}", ex.Message);
                throw;
            }
        }

        public static void ExecuteInPostgresTransaction(
            this IDbConnectionFactory factory, 
            Action<IDbConnection, IDbTransaction> action,
            ILogger? logger = null)
        {
            factory.ExecuteInPostgresTransaction((conn, tx) =>
            {
                action(conn, tx);
                return true;
            }, logger);
        }
    }
}