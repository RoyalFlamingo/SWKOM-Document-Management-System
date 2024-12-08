using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Business.Models.Domain;
using System.Text.Json;

namespace Business.Services;

public class RabbitMqListenerService : IHostedService
{
	private IConnection _connection;
	private IModel _channel;

	private readonly IElasticService _elasticService;

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await Task.Run(() =>
		{
			ConnectToRabbitMQ();
			StartListening();
		}, cancellationToken);

		Console.WriteLine("RabbitMqListenerService gestartet.");
	}

	public RabbitMqListenerService(IElasticService elasticService)
	{
		_elasticService = elasticService;
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
		var consumer = new EventingBasicConsumer(_channel);
		consumer.Received += (model, ea) =>
		{
			_ = Task.Run(async () =>
			{
				var body = ea.Body.ToArray();
				var message = Encoding.UTF8.GetString(body);

				Console.WriteLine($@"Result queue message received: {message}");

				try
				{
					var options = new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true,
						NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
					};

					var document = JsonSerializer.Deserialize<Document>(message, options);

					if (document != null && !string.IsNullOrWhiteSpace(document.OcrContent))
					{
						Console.WriteLine($"[x] Received OCR content for ID: {document.Id}");

						await _elasticService.IndexDocument(document);

						Console.WriteLine($"[x] Successfully indexed document with ID: {document.Id}");
					}
					else
					{
						Console.WriteLine("[!] Received invalid document or empty OCR content.");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[!] Failed to process message: {ex.Message}");
				}
			});
		};

		_channel.BasicConsume(queue: "ocr_result_queue", autoAck: true, consumer: consumer);
	}


	public Task StopAsync(CancellationToken cancellationToken)
	{
		_channel?.Close();
		_connection?.Close();
		return Task.CompletedTask;
	}
}


