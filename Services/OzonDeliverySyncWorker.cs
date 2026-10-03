namespace ApiOzon.Services
{
    public class OzonDeliverySyncWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OzonSyncTrigger _trigger;

        public OzonDeliverySyncWorker(IServiceScopeFactory scopeFactory, OzonSyncTrigger trigger)
        {
            _scopeFactory = scopeFactory;
            _trigger = trigger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("OZON WORKER: запущен и ожидает команды...");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Сидим и ждем вызова из контроллера. Поток спит.
                    if (await _trigger.WaitAsync(stoppingToken))
                    {
                        _trigger.Clear(); // Очищаем сигнал

                        using var scope = _scopeFactory.CreateScope();
                        var service = scope.ServiceProvider.GetRequiredService<OzonDeliverySyncService>();

                        // Запускаем службу синхронизации
                        await service.RunAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"OZON WORKER CRITICAL ERROR: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }
    }
}
