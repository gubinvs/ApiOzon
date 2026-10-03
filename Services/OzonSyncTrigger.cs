using System.Threading.Channels;

namespace ApiOzon.Services
{
    public class OzonSyncTrigger
    {
        // Очередь с емкостью 1, чтобы нельзя было заспамить кликами
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite // Игнорируем повторные клики, если уже выполняется
        });

        // Метод для контроллера
        public void FireSync()
        {
            _channel.Writer.TryWrite(true);
        }

        // Метод для воркера
        public ValueTask<bool> WaitAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.WaitToReadAsync(cancellationToken);
        }

        // Метод для очистки признака после прочтения воркером
        public void Clear()
        {
            _channel.Reader.TryRead(out _);
        }
    }
}
