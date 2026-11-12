using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BuzaiManagementApi.Models;
using BuzaiManagementApi.Repositories;
using Serilog;
using Serilog.Events;

// ユーザーのAppData\Local\COMPLEMENTARY パスを動的に構築
string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string localAppDataDir = Path.Combine(userProfile, "AppData", "Local", "COMPLEMENTARY");
string localAppDataLogsDir = Path.Combine(localAppDataDir, "Logs");
Directory.CreateDirectory(localAppDataLogsDir);

// Serilogによるファイル出力（詳細）およびコンソール出力（最小限）の設定
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt => 
            evt.Level >= LogEventLevel.Error || 
            (evt.Properties.ContainsKey("SourceContext") && evt.Properties["SourceContext"].ToString().Contains("BuzaiManagementApi")) ||
            evt.MessageTemplate.Text.Contains("アプリケーション") ||
            evt.MessageTemplate.Text.Contains("Webアプリケーション"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    )
    .WriteTo.File(
        path: Path.Combine(localAppDataLogsDir, "api_debug_log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

Log.Information("アプリケーションの接続・起動処理を開始します。");

// --- AppData\Local\COMPLEMENTARY 配下の local.config.txt (XML形式) からモードを読み込む ---
string localConfigFilePath = Path.Combine(localAppDataDir, "local.config");
string mode = "Test"; // デフォルト値

if (File.Exists(localConfigFilePath))
{
    try
    {
        XDocument xDoc = XDocument.Load(localConfigFilePath);
        var envElement = xDoc.Descendants("add")
                             .FirstOrDefault(e => e.Attribute("key")?.Value?.Equals("Environment", StringComparison.OrdinalIgnoreCase) == true);
        
        if (envElement != null)
        {
            string? val = envElement.Attribute("value")?.Value?.Trim();
            if (!string.IsNullOrEmpty(val))
            {
                mode = val;
            }
        }
        Log.Information("設定ファイルから環境モードの読み込みに成功: [{Mode}] (パス: {Path})", mode, localConfigFilePath);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "設定ファイルの読み込みに失敗しました。デフォルトのモード [{DefaultMode}] を使用します。", mode);
    }
}
else
{
    Log.Warning("指定された設定ファイルが存在しません ({Path})。デフォルトのモード [{DefaultMode}] を使用します。", localConfigFilePath, mode);
}

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", mode);
Log.Information("適用されたモード: [{Mode}]", mode);

// --- 設定ファイル（.ini）および Reader.txt のパス決定部分 ---
bool isHonban = mode.Equals("Honban", StringComparison.OrdinalIgnoreCase);

string configPath;
string readerTxtPath = @"\\192.168.0.10\4.改善室\★改善室\A05.config\Reader";

if (isHonban)
{
    configPath = @"\\192.168.0.10\4.改善室\★改善室\A05.config\postgres_addin.ini";
}
else
{
    configPath = Path.Combine(localAppDataDir, "postgres_addin_Test.ini");
    if (!File.Exists(configPath))
    {
        configPath = Path.Combine(AppContext.BaseDirectory, "postgres_addin_Test.ini");
    }
}

Log.Information("探索中の設定ファイルパス: {Path}", configPath);
Log.Information("探索中の Reader.txt パス: {Path}", readerTxtPath);

// --- 起動時に Reader.txt を読み込んでメモリにキャッシュする処理 ---
var cachedDeviceMappings = new System.Collections.Generic.List<DeviceMappingDto>();
if (File.Exists(readerTxtPath))
{
    try
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] readerBytes = File.ReadAllBytes(readerTxtPath);
        string readerContent;
        try
        {
            readerContent = Encoding.GetEncoding(932).GetString(readerBytes);
        }
        catch
        {
            readerContent = Encoding.UTF8.GetString(readerBytes);
        }

        readerContent = readerContent.Replace("\uFEFF", "").Replace("\u200B", "");
        var readerLines = readerContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (var rawLine in readerLines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            var parts = line.Split(new[] { ',', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .ToArray();

            if (parts.Length >= 2)
            {
                cachedDeviceMappings.Add(new DeviceMappingDto
                {
                    TargetIpAddress = parts[0],
                    TargetMacAddress = parts.Length > 1 ? parts[1] : null,
                    TargetMeisho = parts.Length > 2 ? parts[2] : null
                });
            }
        }
        Log.Information("起動時処理: Reader.txt から {Count} 件のデバイスマッピングをキャッシュしました。", cachedDeviceMappings.Count);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Reader.txt の読み込み中にエラーが発生しました: {Message}", ex.Message);
    }
}
else
{
    Log.Warning("指定された Reader.txt が存在しません ({Path})。", readerTxtPath);
}

string serverUrl = "http://0.0.0.0:8080";
int retentionDays = 90;
string rawPgConnStr = "";
string rawSqlServerConnStr = "";

if (File.Exists(configPath))
{
    try
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        byte[] fileBytes = File.ReadAllBytes(configPath);
        
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
                
                // 両端のダブルクォーテーションやシングルクォーテーションを除去
                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                {
                    val = val.Substring(1, val.Length - 2);
                }

                if (currentSection.Equals("Server", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.Equals("ServerUrl", StringComparison.OrdinalIgnoreCase))
                    {
                        serverUrl = val;
                    }
                    else if (key.Equals("LogRetentionDays", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(val, out int parsedDays))
                        {
                            retentionDays = parsedDays;
                        }
                    }
                }
                else if (currentSection.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.Equals("ConnStr", StringComparison.OrdinalIgnoreCase))
                    {
                        rawPgConnStr = val;
                    }
                }
                else if (currentSection.Equals("TECHS", StringComparison.OrdinalIgnoreCase))
                {
                    if (key.Equals("ConnStr", StringComparison.OrdinalIgnoreCase))
                    {
                        rawSqlServerConnStr = val; // ★ ここで正しくTECHSの接続文字列を受け取る
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "設定ファイルの読み込み中にエラーが発生しました: {Message}", ex.Message);
    }
}

// PostgreSQLの接続文字列を組み立てる
string server = ExtractParam(rawPgConnStr, new[] { "Server", "Host" });
string port = ExtractParam(rawPgConnStr, new[] { "Port" }, "5432");
string database = ExtractParam(rawPgConnStr, new[] { "Database", "Initial Catalog" });
string uidVal = ExtractParam(rawPgConnStr, new[] { "Uid", "Username", "User ID" });
string pwd = ExtractParam(rawPgConnStr, new[] { "Pwd", "Password" });

string postgresConnStr = $"Host={server};Port={port};Database={database};Username={uidVal};Password={pwd};Timeout=3;";

// TECHS (SQL Server) の接続文字列を設定（INIに指定があればそれを使い、なければデフォルト）
string sqlServerConnStr;
if (!string.IsNullOrEmpty(rawSqlServerConnStr))
{
    // Provider=SQLNCLI11; などの記述を自動で削除する
    sqlServerConnStr = Regex.Replace(rawSqlServerConnStr, @"Provider\s*=\s*[^;]+;?", "", RegexOptions.IgnoreCase);
    
    // 必要に応じて TrustServerCertificate=True が無ければ追加
    if (!sqlServerConnStr.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
    {
        sqlServerConnStr = sqlServerConnStr.TrimEnd(';') + ";TrustServerCertificate=True;";
    }
}
else
{
    // INIに指定がない場合のデフォルト値
    sqlServerConnStr = "Server=techs-sv;Database=TECHS6;Trusted_Connection=True;TrustServerCertificate=True;";
}

Log.Information("最終決定された ServerUrl: {Url}, 保存日数: {Days}日", serverUrl, retentionDays);
Log.Information("使用する TECHS 接続文字列: {ConnStr}", sqlServerConnStr);

// ロガーの再構成
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt => 
            evt.Level >= LogEventLevel.Error || 
            evt.MessageTemplate.Text.Contains("アプリケーション") ||
            evt.MessageTemplate.Text.Contains("Webアプリケーション"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    )
    .WriteTo.File(
        path: Path.Combine(localAppDataLogsDir, "api_debug_log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: retentionDays,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Windowsサービスとして動作させる設定
builder.Host.UseWindowsService();

builder.Environment.EnvironmentName = mode;
builder.WebHost.UseUrls(serverUrl);

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// 解決済みの接続文字列を DbConnectionFactory に渡して登録
builder.Services.AddSingleton<IDbConnectionFactory>(sp => 
    new DbConnectionFactory(
        sp.GetRequiredService<ILogger<DbConnectionFactory>>(), 
        sqlServerConnStr, 
        postgresConnStr
    )
);

// 起動時にキャッシュしたデバイスマッピング情報をシングルトンとしてDIに登録
builder.Services.AddSingleton<System.Collections.Generic.IReadOnlyList<DeviceMappingDto>>(cachedDeviceMappings);

builder.Services.AddTransient<SKL0001G01Repository>();
builder.Services.AddScoped<SKL0101G01Repository>();
builder.Services.AddScoped<SKL0110G01Repository>();
builder.Services.AddScoped<SKL0111G01Repository>();
builder.Services.AddScoped<CommonRepository>();
builder.Services.AddScoped<SKL0104G01Repository>();
builder.Services.AddScoped<BarcodeReaderMasterRepository>();

// 購入品受入検査システム用のリポジトリを追加
builder.Services.AddScoped<AcceptanceRepository>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

try
{
    Log.Information("Webアプリケーションを起動します（URL: {Url}）", serverUrl);
    app.Run();
    Log.Information("Webアプリケーションが正常に終了しました。");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Webアプリケーションの実行中に致命的なエラーが発生しました: {Message}", ex.Message);
    throw;
}

// 補助メソッド: INIの接続文字列からパラメータを抽出する
string ExtractParam(string connStr, string[] keys, string defaultValue = "")
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