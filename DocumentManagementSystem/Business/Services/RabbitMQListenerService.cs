using AutoMapper;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Persistence.Models;

namespace Business.Services;

public class RabbitMqListenerService : IHostedService
{
	private IConnection _connection;
	private IModel _channel;
	public Task StartAsync(CancellationToken cancellationToken)
	{
		ConnectToRabbitMQ();
		StartListening();
		return Task.CompletedTask;
	}

	public RabbitMqListenerService()
	{
	}

	private void ConnectToRabbitMQ()
	{
		int retries = 5;
		while (retries > 0)
		{
			try
			{
				var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = "karo", Password = "karo" };
				_connection = factory.CreateConnection();
				_channel = _connection.CreateModel();

				_channel.QueueDeclare(queue: "ocr_result_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
				Console.WriteLine("Erfolgreich mit RabbitMQ verbunden und Queue erstellt.");

				break;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fehler beim Verbinden mit RabbitMQ: {ex.Message}. Versuche es in 5 Sekunden erneut...");
				Thread.Sleep(5000);
				retries--;
			}
		}

		if (_connection == null || !_connection.IsOpen)
		{
			throw new Exception("Konnte keine Verbindung zu RabbitMQ herstellen, alle Versuche fehlgeschlagen.");
		}
	}

	private void StartListening()
	{
		try
		{
			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += async (model, ea) =>
			{
				var body = ea.Body.ToArray();
				var message = Encoding.UTF8.GetString(body);
				var parts = message.Split('|');

				Console.WriteLine($@"Result queue message received: {message}");

				if (parts.Length == 2)
				{
					var id = parts[0];
					var extractedText = parts[1];
					if (string.IsNullOrEmpty(extractedText))
					{
						Console.WriteLine($@"Error: No OCR text for message {id}, ignoring message.");
						return;
					}


				}
				else
				{
					Console.WriteLine(@"Error, invalid message received.");
				}
			};

			_channel.BasicConsume(queue: "ocr_result_queue", autoAck: true, consumer: consumer);
		}
		catch (Exception ex)
		{
			Console.WriteLine($@"Error launching listener for result queue: {ex.Message}");
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		_channel?.Close();
		_connection?.Close();
		return Task.CompletedTask;
	}
}
