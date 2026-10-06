using System.IO;
using System.Text;
using System.Xml.Linq;
using BuzaiManagementApi.Repositories;
using Serilog;

// ==========================================
// 1. 環境判定ファイルとXMLから設定を動的読み込み
// ==========================================
string envModeFilePath = Path.Combine(AppContext.BaseDirectory, "env_mode.txt");
string mode = "Test";

if (File.Exists(envModeFilePath))
{
    try { mode = File.ReadAllText(envModeFilePath).Trim(); } catch { }
}

// ASP.NET Coreが最初に見る環境変数を強制的に上書きする
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", mode);

Console.WriteLine($"[DEBUG] 読み込んだモード: [{mode}] (ファイルパス: {envModeFilePath})");

// "Honban" かどうかでモード判定してXMLファイルのパスを決定
bool isHonban = mode.Equals("Honban", StringComparison.OrdinalIgnoreCase);

string configPath = isHonban
    ? @"C:\4.改善室\★改善室\A04.config\postgres_addin.config"
    : Path.Combine(AppContext.BaseDirectory, "postgres_addin_Test.config");

Console.WriteLine($"[DEBUG] 探索中の設定ファイルパス: {configPath}");
Console.WriteLine($"[DEBUG] ファイルの存在有無: {File.Exists(configPath)}");

// XMLファイルから起動用URL（IPアドレス）を読み込む（ファイルがない場合のデフォルト値も用意）
string serverUrl = "http://192.168.3.6:8080"; 
if (File.Exists(configPath))
{
    try
    {
        // Shift-JIS（コードページ 932）を指定してテキストとして読み込む（SJIS対策）
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        string xmlContent = File.ReadAllText(configPath, Encoding.GetEncoding(932));
        
        // 文字列をXMLとしてパース
        var doc = XDocument.Parse(xmlContent);
        
        var urlSetting = doc.Descendants("appSettings")
            .Elements("add")
            .FirstOrDefault(e => e.Attribute("key")?.Value == "ServerUrl")?
            .Attribute("value")?.Value;

        if (!string.IsNullOrEmpty(urlSetting))
        {
            serverUrl = urlSetting;
            Console.WriteLine($"[DEBUG] XMLからURLを取得成功: {serverUrl}");
        }
        else
        {
            Console.WriteLine("[DEBUG WARNING] XML内に ServerUrl が見つかりませんでした。");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DEBUG ERROR] XMLの読み込みでエラー発生: {ex.Message}");
    }
}
else
{
    Console.WriteLine("[DEBUG ERROR] 指定されたパスに設定ファイルが存在しません。");
}

Console.WriteLine($"[DEBUG] 最終決定された ServerUrl: {serverUrl}");

var builder = WebApplication.CreateBuilder(args);

// モード名をASP.NET Coreの環境名に反映
builder.Environment.EnvironmentName = mode;

// 読み込んだURL（IPアドレス）をASP.NET Coreに適用
builder.WebHost.UseUrls(serverUrl);

// ==========================================
// 2. Serilog のファイル出力設定
// ==========================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "Logs/api_debug_log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// データベース接続ファクトリの登録
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// リポジトリの登録
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

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    return Enumerable.Range(1, 5).Select(index => new WeatherForecast(
        DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
        Random.Shared.Next(-20, 55),
        summaries[Random.Shared.Next(summaries.Length)]
    )).ToArray();
}).WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}