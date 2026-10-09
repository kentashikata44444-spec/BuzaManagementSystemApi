using System;
using System.Data;
using System.IO;
using System.Linq;
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

        // DbConnectionFactory.cs の該当部分を変更
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

            // ユーザーの AppData\Local\COMPLEMENTARY パスを動的に構築
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localAppDataDir = Path.Combine(userProfile, "AppData", "Local", "COMPLEMENTARY");

            if (_isHonban)
            {
                _configPath = @"\\192.168.0.10\4.改善室\★改善室\A05.config\postgres_addin.ini";
                _logger.LogInformation("本番モード（Honban）の接続設定ファイルを採用しました: {Path}", _configPath);
            }
            else
            {
                // Testモード時は AppData\Local\COMPLEMENTARY 配下を見るように変更
                Directory.CreateDirectory(localAppDataDir);
                _configPath = Path.Combine(localAppDataDir, "postgres_addin_Test.ini");
                _logger.LogInformation("テストモード（Test）の接続設定ファイルを採用しました: {Path}", _configPath);
            }

            LoadConfigOnce();
        }

        private void LoadConfigOnce()
        {
            lock (_lockObj)
            {
                // すでにキャッシュされている場合は再読み込みしない
                if (!string.IsNullOrEmpty(_cachedPostgresConnStr) && !string.IsNullOrEmpty(_cachedSqlServerConnStr))
                {
                    return;
                }

                if (!File.Exists(_configPath))
                {
                    if (!_isHonban)
                    {
                        _logger.LogWarning("テスト設定ファイルが見つからないため、デフォルトファイルの自動作成を行います: {Path}", _configPath);
                        CreateDefaultConfigFile(_configPath);
                    }
                    else
                    {
                        throw new FileNotFoundException($"本番用の設定ファイルが見つかりません: {_configPath}");
                    }
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
                    string rawSqlConnStr = "";

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

                        if (line.StartsWith("ConnStr", StringComparison.OrdinalIgnoreCase))
                        {
                            int eqIndex = line.IndexOf('=');
                            if (eqIndex >= 0)
                            {
                                string val = line.Substring(eqIndex + 1).Trim();
                                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }

                                if (currentSection.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawPgConnStr = val;
                                }
                                else if (currentSection.Equals("SQLServer", StringComparison.OrdinalIgnoreCase))
                                {
                                    rawSqlConnStr = val;
                                }
                            }
                        }
                    }

                    // PostgreSQL 接続文字列の構築とキャッシュ
                    if (!string.IsNullOrEmpty(rawPgConnStr))
                    {
                        var server = ExtractParam(rawPgConnStr, "Server");
                        var port = ExtractParam(rawPgConnStr, "Port", "5432");
                        var database = ExtractParam(rawPgConnStr, "Database");
                        var uid = ExtractParam(rawPgConnStr, "Uid");
                        var pwd = ExtractParam(rawPgConnStr, "Pwd");

                        _cachedPostgresConnStr = $"Host={server};Port={port};Database={database};Username={uid};Password={pwd};Timeout=3;";
                    }

                    // SQLServer 接続文字列の構築とキャッシュ
                    if (!string.IsNullOrEmpty(rawSqlConnStr))
                    {
                        var dataSource = ExtractParam(rawSqlConnStr, "Data Source");
                        var initialCatalog = ExtractParam(rawSqlConnStr, "Initial Catalog");
                        var userId = ExtractParam(rawSqlConnStr, "User ID");
                        var password = ExtractParam(rawSqlConnStr, "Password");

                        _cachedSqlServerConnStr = $"Server={dataSource};Database={initialCatalog};User Id={userId};Password={password};Connection Timeout=3;TrustServerCertificate=true;";
                    }

                    _logger.LogInformation("設定ファイルの初回読み込みおよびキャッシュ化が完了しました。");
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
            // 2回目以降はファイルへアクセスせず、メモリ上のキャッシュを返す
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

        private string ExtractParam(string connStr, string key, string defaultValue = "")
        {
            var pattern = $@"(?:^|;)\s*{key}\s*=\s*([^;]+)";
            var match = Regex.Match(connStr, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return defaultValue;
        }

        private void CreateDefaultConfigFile(string path) { }

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