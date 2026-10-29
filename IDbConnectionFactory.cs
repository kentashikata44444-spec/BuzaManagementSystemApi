using System;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly string _configPath;
        private readonly bool _isHonban;

        // 初回読み込みした接続文字列を保持するキャッシュ用フィールド
        private static string? _cachedPostgresConnStr;
        private static string? _cachedSqlServerConnStr;
        private static readonly object _lockObj = new object();

        public DbConnectionFactory(ILogger<DbConnectionFactory> logger)
        {
            _logger = logger;

            string envModeFilePath = Path.Combine(AppContext.BaseDirectory, "env_mode.txt");
            string mode = "Test";

            if (File.Exists(envModeFilePath))
            {
                try { mode = File.ReadAllText(envModeFilePath).Trim(); } catch { }
            }

            _isHonban = mode.Equals("Honban", StringComparison.OrdinalIgnoreCase);

            // テスト時はプロジェクト直下（またはアプリベースディレクトリ）の postgres_addin_Test.ini を指定
            string projectRootIniPath = Path.Combine(AppContext.BaseDirectory, "../../../postgres_addin_Test.ini");
            string fallbackBinIniPath = Path.Combine(AppContext.BaseDirectory, "postgres_addin_Test.ini");

            if (_isHonban)
            {
                _configPath = @"\\192.168.0.10\4.改善室\★改善室\A05.config\postgres_addin.ini";
                _logger.LogInformation("本番モード（Honban）の接続設定ファイルを採用しました: {Path}", _configPath);
            }
            else
            {
                _configPath = File.Exists(projectRootIniPath) ? Path.GetFullPath(projectRootIniPath) : fallbackBinIniPath;
                _logger.LogInformation("テストモード（Test）の接続設定ファイルを採用しました: {Path}", _configPath);
            }

            LoadConfigOnce();
        }

        private void LoadConfigOnce()
        {
            lock (_lockObj)
            {
                if (!string.IsNullOrEmpty(_cachedPostgresConnStr) && !string.IsNullOrEmpty(_cachedSqlServerConnStr))
                {
                    return;
                }

                // --- 1. SQL Server (TECHS) 接続文字列はテスト・本番問わず固定の安全な値を使用 ---
                _cachedSqlServerConnStr = "Server=techs-sv;Database=TECHS6;Trusted_Connection=True;TrustServerCertificate=True;";
                _logger.LogInformation("SQL Server接続文字列には固定値を使用します: {ConnStr}", _cachedSqlServerConnStr);

                // --- 2. PostgreSQL は引き続き ini ファイルから読み込む ---
                if (!File.Exists(_configPath))
                {
                    throw new FileNotFoundException($"設定ファイルが見つかりません: {_configPath}");
                }

                try
                {
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    byte[] fileBytes = File.ReadAllBytes(_configPath);
                    string fileContent;
                    try
                    {
                        fileContent = Encoding.GetEncoding(932).GetString(fileBytes);
                    }
                    catch
                    {
                        fileContent = Encoding.UTF8.GetString(fileBytes);
                    }

                    fileContent = fileContent.Replace("\uFEFF", "").Replace("\u200B", "");

                    string currentSection = "";
                    string rawPgConnStr = "";

                    var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    foreach (var rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                            continue;

                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line.Substring(1, line.Length - 2).Trim();
                            continue;
                        }

                        int eqIndex = line.IndexOf('=');
                        if (eqIndex >= 0)
                        {
                            string key = line.Substring(0, eqIndex).Trim();
                            string val = line.Substring(eqIndex + 1).Trim();
                            if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                            {
                                val = val.Substring(1, val.Length - 2);
                            }

                            if (key.Equals("ConnStr", StringComparison.OrdinalIgnoreCase))
                            {
                                if (currentSection.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawPgConnStr = val;
                                }
                            }
                        }
                    }

                    // PostgreSQL 接続文字列の構築とキャッシュ
                    if (!string.IsNullOrEmpty(rawPgConnStr))
                    {
                        var server = ExtractParam(rawPgConnStr, new[] { "Server", "Host" });
                        var port = ExtractParam(rawPgConnStr, new[] { "Port" }, "5432");
                        var database = ExtractParam(rawPgConnStr, new[] { "Database", "Initial Catalog" });
                        var uidVal = ExtractParam(rawPgConnStr, new[] { "Uid", "Username", "User ID" });
                        var pwd = ExtractParam(rawPgConnStr, new[] { "Pwd", "Password" });

                        _cachedPostgresConnStr = $"Host={server};Port={port};Database={database};Username={uidVal};Password={pwd};Timeout=3;";
                    }

                    _logger.LogInformation("設定ファイルからの PostgreSQL 接続文字列の読み込みおよびキャッシュ化が完了しました。");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "接続文字列の読み込み中にエラーが発生しました（ファイル: {Path}）: {Message}", _configPath, ex.Message);
                    throw;
                }
            }
        }

        private string GetConnectionString(string name)
        {
            if (name == "PostgresConnection")
            {
                if (string.IsNullOrEmpty(_cachedPostgresConnStr))
                    throw new InvalidOperationException("PostgreSQLの接続文字列がキャッシュされていません。");
                return _cachedPostgresConnStr;
            }
            else if (name == "SqlServerConnection")
            {
                if (string.IsNullOrEmpty(_cachedSqlServerConnStr))
                    throw new InvalidOperationException("SQLServerの接続文字列がキャッシュされていません。");
                return _cachedSqlServerConnStr;
            }

            throw new ArgumentException($"不明な接続名です: {name}");
        }

        private string ExtractParam(string connStr, string[] keys, string defaultValue = "")
        {
            foreach (var key in keys)
            {
                var pattern = $@"(?:^|;)\s*{key}\s*=\s*([^;]+)";
                var match = Regex.Match(connStr, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }
            return defaultValue;
        }

        public IDbConnection CreateSqlServerConnection()
        {
            var connStr = GetConnectionString("SqlServerConnection");
            return new SqlConnection(connStr);
        }

        public IDbConnection CreatePostgresConnection()
        {
            var connStr = GetConnectionString("PostgresConnection");
            return new NpgsqlConnection(connStr);
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