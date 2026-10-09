using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BuzaiManagementApi.Repositories;
using Serilog;
using Serilog.Events;

// ユーザーのAppData\Local\COMPLEMENTARY\Logs パスを動的に構築
string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
string localAppDataLogsDir = Path.Combine(userProfile, "AppData", "Local", "COMPLEMENTARY", "Logs");
Directory.CreateDirectory(localAppDataLogsDir);

// Serilogによるファイル出力（詳細）およびコンソール出力（最小限）の設定
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    // コンソールには自作の最小限ログ（Information以上、かつMicrosoft等のフレームワークログを除外）のみ出力する
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt => 
            evt.Level >= LogEventLevel.Error || 
            (evt.Properties.ContainsKey("SourceContext") && evt.Properties["SourceContext"].ToString().Contains("BuzaiManagementApi")) ||
            evt.MessageTemplate.Text.Contains("アプリケーション") ||
            evt.MessageTemplate.Text.Contains("Webアプリケーション"))
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    )
    // ファイルには従来通りすべて出力し、shared: true で日付ごとに1ファイルへ追記
    .WriteTo.File(
        path: Path.Combine(localAppDataLogsDir, "api_debug_log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

Log.Information("アプリケーションの接続・起動処理を開始します。");

string envModeFilePath = Path.Combine(AppContext.BaseDirectory, "env_mode.txt");
string mode = "Test";

if (File.Exists(envModeFilePath))
{
    try { mode = File.ReadAllText(envModeFilePath).Trim(); } catch { }
}

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", mode);
Log.Information("読み込んだモード: [{Mode}] (ファイルパス: {Path})", mode, envModeFilePath);

// --- 設定ファイルのパス決定部分 ---
bool isHonban = mode.Equals("Honban", StringComparison.OrdinalIgnoreCase);

// userProfile は上で既に宣言されているので、そのまま再利用します
string localAppDataDir = Path.Combine(userProfile, "AppData", "Local", "COMPLEMENTARY");
Directory.CreateDirectory(localAppDataDir);

string configPath = isHonban
    ? @"\\192.168.0.10\4.改善室\★改善室\A05.config\postgres_addin.ini"
    : Path.Combine(localAppDataDir, "postgres_addin_Test.ini");

Log.Information("探索中の設定ファイルパス: {Path}", configPath);
Log.Information("ファイルの存在有無: {Exists}", File.Exists(configPath));

string serverUrl = "http://0.0.0.0:8080";
int retentionDays = 90; // デフォルト90日

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

        // BOMやゼロ幅スペース、CRLFの正規化
        fileContent = fileContent.Replace("\uFEFF", "").Replace("\u200B", "");

        // 実際に読んでいるファイルの内容をログに出力
        Log.Information("=== [CONFIG FILE CONTENT START] ===\n{Content}\n=== [CONFIG FILE CONTENT END] ===", fileContent);

        // --- 本番・テスト共通でINI形式としてパースする ---
        string currentSection = "";
        var lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        Log.Information("INIファイル総行数: {Count}", lines.Length);

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                Log.Information("セクション検出: [{Section}]", currentSection);
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
                        Log.Information("★ [Server] から ServerUrl の取得に成功: {Url}", serverUrl);
                    }
                    else if (key.Equals("LogRetentionDays", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(val, out int parsedDays))
                        {
                            retentionDays = parsedDays;
                            Log.Information("★ [Server] から LogRetentionDays の取得に成功: {Days}日", retentionDays);
                        }
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

Log.Information("最終決定された ServerUrl: {Url}, 保存日数: {Days}日", serverUrl, retentionDays);

// ロガーの再構成（コンソール・ファイルの設定を維持）
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

// ASP.NET Core標準のコンソールロギングを抑制し、Serilogに一本化
builder.Logging.ClearProviders();
builder.Host.UseSerilog();

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

builder.Services.AddTransient<SKL0001G01Repository>();
builder.Services.AddScoped<SKL0101G01Repository>();
builder.Services.AddScoped<CommonRepository>();

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