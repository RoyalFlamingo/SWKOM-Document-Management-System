using AutoMapper;
using Business.Models.Domain;
using Business.Models.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.Models.Entities;
using RabbitMQ.Client;
using System.Text;

namespace Business.Services;

public static class RabbitMQQueues
{
	public const string FileQueue = "file_queue";
}

public class RabbitMQService : IRabbitMQService
{
	private readonly ILogger<DocumentService> _logger;
	private readonly RabbitMQConfig _rabbitMQConfig;
	private readonly IConnection _connection;
	private readonly IModel _channel;

	public RabbitMQService(ILogger<DocumentService> logger, IOptions<RabbitMQConfig> rabbitMQConfig)
	{
		_logger = logger;
		_rabbitMQConfig = rabbitMQConfig.Value;

		var factory = new ConnectionFactory()
		{
			HostName = _rabbitMQConfig.HostName,
			UserName = _rabbitMQConfig.UserName,
			Password = _rabbitMQConfig.Password,
		};

		_connection = factory.CreateConnection();
		_channel = _connection.CreateModel();
	}

	public void SendMessage(string queueName, string message)
	{
		_channel.QueueDeclare(queue: queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

		var body = Encoding.UTF8.GetBytes(message);

		_channel.BasicPublish(exchange: "", routingKey: queueName, basicProperties: null, body: body);
	}

	public void Dispose()
	{
		_channel?.Close();
		_connection?.Close();
	}
}

