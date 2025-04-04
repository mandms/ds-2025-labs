namespace Valuator.Services
{
    public interface IRabbitMQService
    {
        void SendTextMessage(string textId, CancellationTokenSource cts);
		void SendSimilarityMessage(bool similarity, string id, CancellationTokenSource cts);
	}
}
