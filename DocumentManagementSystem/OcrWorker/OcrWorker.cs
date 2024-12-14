using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using ImageMagick;
using Tesseract;
using System.Diagnostics;

namespace OCRWorker
{
	public class OcrWorker
	{
		private IConnection _connection;
		private IModel _channel;

		public OcrWorker()
		{
			ConnectToRabbitMQ();
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

					_channel.QueueDeclare(queue: "file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
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

		public void Start()
		{
			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (model, ea) =>
			{
				var body = ea.Body.ToArray();
				var message = Encoding.UTF8.GetString(body);
				var parts = message.Split('|');

				if (parts.Length == 3)
				{
					var id = parts[0];
					var filePath = parts[1];
					var fileName = parts[2];

					Console.WriteLine($"[x] Received ID: {id}, FilePath: {filePath}, FileName: {fileName}");

					if (!File.Exists(filePath))
					{
						Console.WriteLine($"Fehler: Datei {filePath} nicht gefunden.");
						return;
					}

					var extractedText = PerformOcr(filePath);

					if (!string.IsNullOrEmpty(extractedText))
					{
						var resultMessage = new
						{
							Id = id,
							Name = fileName,
							OcrContent = extractedText
						};

						string jsonResult = System.Text.Json.JsonSerializer.Serialize(resultMessage);
						var resultBody = Encoding.UTF8.GetBytes(jsonResult);

						_channel.BasicPublish(exchange: "", routingKey: "ocr_result_queue", basicProperties: null, body: resultBody);

						Console.WriteLine($"[x] Sent OCR result for ID: {id}");
					}
				}
				else
				{
					Console.WriteLine("Fehler: Ungültige Nachricht empfangen, Split in weniger als 2 Teile.");
				}
			};

			_channel.BasicConsume(queue: "file_queue", autoAck: true, consumer: consumer);
		}

		private string PerformOcr(string filePath)
		{
			var stringBuilder = new StringBuilder();

			try
			{
				using (var images = new MagickImageCollection(filePath))
				{
					foreach (var image in images)
					{
						var tempPngFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");

						image.Density = new Density(300, 300);
						image.Format = MagickFormat.Png;
						image.Write(tempPngFile);

						var psi = new ProcessStartInfo
						{
							FileName = "tesseract",
							Arguments = $"{tempPngFile} stdout -l eng",
							RedirectStandardOutput = true,
							UseShellExecute = false,
							CreateNoWindow = true
						};

						using (var process = Process.Start(psi))
						{
							string result = process.StandardOutput.ReadToEnd();
							stringBuilder.AppendLine(result.Trim());
						}

						File.Delete(tempPngFile);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"OCR error: {ex.Message}");
			}

			return stringBuilder.ToString().Trim();
		}


		public void Dispose()
		{
			_channel?.Close();
			_connection?.Close();
		}
	}
}