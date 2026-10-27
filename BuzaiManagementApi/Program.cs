using BuzaiManagementApi;
using BuzaiManagementApi.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 「すべて（0.0.0.0）」で待ち受ける設定
builder.WebHost.UseUrls("http://0.0.0.0:8080");

// ★【追加】 Web APIをWindowsサービスとして動作させるための設定
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "BuzaiManagementService"; // Windowsサービス一覧に表示される名前
});

// データベース接続ファクトリの登録
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
// SKL0001G01 用リポジトリの登録
builder.Services.AddTransient<SKL0001G01Repository>();
// ★【追加】 WorkerクラスをホステッドサービスとしてDIコンテナに登録
builder.Services.AddHostedService<Worker>();
// コントローラー機能の有効化
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddOpenApi();
// ★ 【追加】 CommonRepository をDIコンテナに登録する
builder.Services.AddScoped<CommonRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ★重要：コントローラーのエンドポイントをマッピングする
app.MapControllers();

app.MapGet("/weatherforecast", () =>
{
    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// ポート番号やリッスンするアドレスを明示的に固定する
app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}