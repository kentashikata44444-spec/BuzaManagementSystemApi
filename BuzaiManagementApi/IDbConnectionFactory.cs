using System;
using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Npgsql;

public interface IDbConnectionFactory
{
    IDbConnection CreateSqlServerConnection();
    IDbConnection CreatePostgresConnection();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _config;

    public DbConnectionFactory(IConfiguration config)
    {
        _config = config;
    }

    public IDbConnection CreateSqlServerConnection()
    {
        var connStr = _config.GetConnectionString("SqlServerConnection");
        return new SqlConnection(connStr);
    }

    public IDbConnection CreatePostgresConnection()
    {
        var connStr = _config.GetConnectionString("PostgresConnection");
        return new NpgsqlConnection(connStr);
    }
}

// ★ ここにトランザクションを共通処理化する拡張メソッドを追加する
public static class DbConnectionFactoryExtensions
{
    /// <summary>
    /// PostgreSQL接続とトランザクションを自動管理しながら処理を実行する共通メソッド
    /// </summary>
    public static T ExecuteInPostgresTransaction<T>(
        this IDbConnectionFactory factory, 
        Func<IDbConnection, IDbTransaction, T> action)
    {
        using var conn = factory.CreatePostgresConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            // 処理を実行
            T result = action(conn, transaction);
            // 成功したらコミット
            transaction.Commit();
            return result;
        }
        catch
        {
            // 失敗したら自動ロールバック
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 戻り値がない（INSERT/UPDATE/DELETEなどの）トランザクション用共通メソッド
    /// </summary>
    public static void ExecuteInPostgresTransaction(
        this IDbConnectionFactory factory, 
        Action<IDbConnection, IDbTransaction> action)
    {
        factory.ExecuteInPostgresTransaction((conn, tx) =>
        {
            action(conn, tx);
            return true; // ダミーの戻り値
        });
    }
}