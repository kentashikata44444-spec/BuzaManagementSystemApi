using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
string localConfigFilePath = Path.Combine(localAppDataDir, "local.config.txt");
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

// --- 設定ファイル（.ini）のパス決定部分 ---
bool isHonban = mode.Equals("Honban", StringComparison.OrdinalIgnoreCase);

string configPath;
if (isHonban)
{
    configPath = @"\\192.168.0.10\4.改善室\★改善室\A05.config\postgres_addin.ini";
}
else
{
    string appDataIniPath = Path.Combine(localAppDataDir, "postgres_addin_Test.ini");
    string projectRootIniPath = Path.Combine(AppContext.BaseDirectory, "../../../postgres_addin_Test.ini");
    string fallbackBinIniPath = Path.Combine(AppContext.BaseDirectory, "postgres_addin_Test.ini");

    if (File.Exists(appDataIniPath))
    {
        configPath = appDataIniPath;
    }
    else if (File.Exists(projectRootIniPath))
    {
        configPath = Path.GetFullPath(projectRootIniPath);
        Log.Warning("指定された AppData 内にファイルが見つからないため、プロジェクト直下のテスト用INIファイルを使用します: {Path}", configPath);
    }
    else
    {
        configPath = fallbackBinIniPath;
        Log.Warning("指定された AppData 内にファイルが見つからないため、実行ディレクトリのテスト用INIファイルを使用します: {Path}", configPath);
    }
}

Log.Information("探索中の設定ファイルパス: {Path}", configPath);
Log.Information("ファイルの存在有無: {Exists}", File.Exists(configPath));

string serverUrl = "http://0.0.0.0:8080";
int retentionDays = 90;
string rawPgConnStr = "";

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
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "設定ファイルの読み込み中にエラーが発生しました: {Message}", ex.Message);
    }
}

// ★ここで postgresConnStr を確実に組み立てる
string server = ExtractParam(rawPgConnStr, new[] { "Server", "Host" });
string port = ExtractParam(rawPgConnStr, new[] { "Port" }, "5432");
string database = ExtractParam(rawPgConnStr, new[] { "Database", "Initial Catalog" });
string uidVal = ExtractParam(rawPgConnStr, new[] { "Uid", "Username", "User ID" });
string pwd = ExtractParam(rawPgConnStr, new[] { "Pwd", "Password" });

string postgresConnStr = $"Host={server};Port={port};Database={database};Username={uidVal};Password={pwd};Timeout=3;";
string sqlServerConnStr = "Server=techs-sv;Database=TECHS6;Trusted_Connection=True;TrustServerCertificate=True;";

Log.Information("最終決定された ServerUrl: {Url}, 保存日数: {Days}日", serverUrl, retentionDays);

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

builder.Environment.EnvironmentName = mode;
builder.WebHost.UseUrls(serverUrl);

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

// ★解決済みの接続文字列を DbConnectionFactory に渡して登録
builder.Services.AddSingleton<IDbConnectionFactory>(sp => 
    new DbConnectionFactory(
        sp.GetRequiredService<ILogger<DbConnectionFactory>>(), 
        sqlServerConnStr, 
        postgresConnStr
    )
);

builder.Services.AddTransient<SKL0001G01Repository>();
builder.Services.AddScoped<SKL0101G01Repository>();
builder.Services.AddScoped<SKL0110G01Repository>();
builder.Services.AddScoped<SKL0111G01Repository>();
builder.Services.AddScoped<CommonRepository>();
builder.Services.AddScoped<SKL0104G01Repository>();
builder.Services.AddScoped<BarcodeReaderMasterRepository>();

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