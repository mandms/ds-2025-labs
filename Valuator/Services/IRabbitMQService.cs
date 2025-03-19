namespace Valuator.Services
{
    public interface IRabbitMQService
    {
        void SendMessage(object obj, CancellationTokenSource cts);
    }
}
