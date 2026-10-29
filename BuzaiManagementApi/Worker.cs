using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuzaiManagementApi
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("サービスが実行中: {time}", DateTimeOffset.Now);

                    // 5秒おきに待機
                    await Task.Delay(5000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // アプリ終了・サービス停止時のキャンセルは正常なため、例外を無視して安全に終了する
                _logger.LogInformation("サービスが正常に停止されました。");
            }
        }
    }
}
