using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;
using Npgsql;

/// <summary>
/// データベース接続インスタンスを生成するためのファクトリインターフェース
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>SQL Server用の接続を作成します。</summary>
    IDbConnection CreateSqlServerConnection();

    /// <summary>PostgreSQL用の接続を作成します。</summary>
    IDbConnection CreatePostgresConnection();
}

/// <summary>
/// 環境判定ファイルと連動して本番・テストの接続文字列ファイルを切り替えるクラス
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly ILogger<DbConnectionFactory> _logger;
    private readonly string _xmlPath;

    public DbConnectionFactory(ILogger<DbConnectionFactory> logger)
    {
        _logger = logger;

        // env_mode.txt からモードを読み取る
        string envModeFilePath = Path.Combine(AppContext.BaseDirectory, "env_mode.txt");
        string mode = "Test";

        if (File.Exists(envModeFilePath))
        {
            try { mode = File.ReadAllText(envModeFilePath).Trim(); } catch { }
        }

        // "Honban" かどうかでパスを切り替え
        if (mode.Equals("Honban", StringComparison.OrdinalIgnoreCase))
        {
            _xmlPath = @"C:\4.改善室\★改善室\A05.config\postgres_addin.config";
            _logger.LogInformation("本番モード（Honban）の接続設定ファイルを採用しました: {Path}", _xmlPath);
        }
        else
        {
            _xmlPath = Path.Combine(AppContext.BaseDirectory, "postgres_addin_Test.config");
            _logger.LogInformation("テストモード（Test）の接続設定ファイルを採用しました: {Path}", _xmlPath);
        }
    }

    /// <summary>
    /// XMLファイルから指定された名前の接続文字列を取得します（存在しない場合は自動作成します）。
    /// </summary>
    private string GetConnectionString(string name)
    {
        if (!File.Exists(_xmlPath))
        {
            _logger.LogWarning("設定ファイルが見つからないため、デフォルトファイルの自動作成を行います: {Path}", _xmlPath);
            CreateDefaultConfigFile(_xmlPath);
        }

        try
        {
            var doc = XDocument.Load(_xmlPath);
            var connString = doc.Descendants("add")
                .FirstOrDefault(e => e.Attribute("name")?.Value == name)?
                .Attribute("connectionString")?.Value;

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException($"設定ファイル '{_xmlPath}' 内に指定された接続文字列 '{name}' が見つからないか、空です。");
            }

            return connString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "接続文字列の読み込み中にエラーが発生しました（ファイル: {Path}）: {Message}", _xmlPath, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// 設定ファイルが存在しない場合に、SQL ServerとPostgreSQL両方の接続設定を含むひな形を作成します。
    /// </summary>
    private void CreateDefaultConfigFile(string path)
    {
        try
        {
            var directoryPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string defaultXml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<configuration>
  <appSettings>
    <add key=""ServerUrl"" value=""http://192.168.3.6:8080"" />
  </appSettings>
  <connectionStrings>
    <!-- SQL Server用 -->
    <add name=""SqlServerConnection"" 
         connectionString=""Server=myServerAddress;Database=TECHS6;Trusted_Connection=True;Connection Timeout=3;"" 
         providerName=""System.Data.SqlClient"" />
         
    <!-- PostgreSQL用 -->
    <add name=""PostgresConnection"" 
         connectionString=""Host=localhost;Username=postgres;Password=352011;Database=DBSV;Port=5432;Search Path=COMPLEMENTARY;Timeout=3;"" 
         providerName=""Npgsql"" />
  </connectionStrings>
</configuration>";

            File.WriteAllText(path, defaultXml);
            _logger.LogInformation("デフォルトの設定ファイルを自動作成しました: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定ファイルの自動作成に失敗しました: {Path}", path);
            throw;
        }
    }

    /// <inheritdoc />
    public IDbConnection CreateSqlServerConnection()
    {
        _logger.LogDebug("SQL Server 接続インスタンスを生成します。（参照設定: {Path}）", _xmlPath);
        var connStr = GetConnectionString("SqlServerConnection");
        return new SqlConnection(connStr);
    }

    /// <inheritdoc />
    public IDbConnection CreatePostgresConnection()
    {
        _logger.LogDebug("PostgreSQL 接続インスタンスを生成します。（参照設定: {Path}）", _xmlPath);
        var connStr = GetConnectionString("PostgresConnection");
        return new NpgsqlConnection(connStr);
    }
}

/// <summary>
/// データベース接続およびトランザクション制御を安全に行うための拡張メソッド群
/// </summary>
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