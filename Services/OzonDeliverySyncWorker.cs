namespace ApiOzon.Services
{
    public class OzonDeliverySyncWorker
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        public OzonDeliverySyncWorker(
            IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            Console.WriteLine(
                "OZON WORKER: запущен.");

            // Даём приложению нормально стартовать
            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var service =
                        scope.ServiceProvider
                            .GetRequiredService<
                                OzonDeliverySyncService>();

                    await service.RunAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "OZON WORKER ERROR:");

                    Console.WriteLine(ex);
                }

                // После завершения синхронизации
                // ждём перед следующим полным циклом.
                try
                {
                    await Task.Delay(
                        TimeSpan.FromHours(1),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            Console.WriteLine(
                "OZON WORKER: остановлен.");
        }
    }
}
