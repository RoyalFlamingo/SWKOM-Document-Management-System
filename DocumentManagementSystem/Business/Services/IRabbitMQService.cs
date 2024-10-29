using Business.Models.Domain;

namespace Business.Services;
public interface IRabbitMQService : IDisposable
{
	void SendMessage(string queueName, string message);

}
