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
		private IModel _channel1;
		private IModel _channel2;

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
					_channel1 = _connection.CreateModel();
					_channel2 = _connection.CreateModel();

					_channel1.QueueDeclare(queue: "file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
					_channel2.QueueDeclare(queue: "ocr_result_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
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
			var consumer = new EventingBasicConsumer(_channel1);
			consumer.Received += (model, ea) =>
			{
				var body = ea.Body.ToArray();
				var message = Encoding.UTF8.GetString(body);
				var parts = message.Split('|');

				if (parts.Length == 2)
				{
					var id = parts[0];
					var filePath = parts[1];

					Console.WriteLine($"[x] Received ID: {id}, FilePath: {filePath}");

					if (!File.Exists(filePath))
					{
						Console.WriteLine($"Fehler: Datei {filePath} nicht gefunden.");
						return;
					}

					var extractedText = PerformOcr(filePath);

					if (!string.IsNullOrEmpty(extractedText))
					{
						var resultBody = Encoding.UTF8.GetBytes($"{id}|{extractedText}");
						_channel2.BasicPublish(exchange: "", routingKey: "ocr_result_queue", basicProperties: null, body: resultBody);

						Console.WriteLine($"[x] Sent result for ID: {id}");
					}
				}
				else
				{
					Console.WriteLine("Fehler: Ungültige Nachricht empfangen, Split in weniger als 2 Teile.");
				}
			};

			_channel1.BasicConsume(queue: "file_queue", autoAck: true, consumer: consumer);
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

						image.Density = new Density(300, 300); // Setze die Auflösung
															   //image.ColorType = ColorType.Grayscale; //Unnötige Farben weg
															   //image.Contrast(); // Erhöht den Kontrast
															   //image.Sharpen(); // Schärft das Bild, um Unschärfen zu reduzieren
															   //image.Despeckle(); // Entfernt Bildrauschen
						image.Format = MagickFormat.Png;
						//image.Resize(image.Width * 2, image.Height * 2); // Vergrößere das Bild um das Doppelte
						// Prüfe, ob eine erhebliche Schräglage vorhanden ist
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
							stringBuilder.Append(result);
						}

						File.Delete(tempPngFile);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fehler bei der OCR-Verarbeitung: {ex.Message}");
				if (ex.InnerException != null)
				{
					Console.WriteLine($"Innere Ausnahme: {ex.InnerException.Message}");
					Console.WriteLine($"Stacktrace: {ex.StackTrace}");
				}
			}

			return stringBuilder.ToString();
		}

		public void Dispose()
		{
			_channel1?.Close();
			_channel2?.Close();
			_connection?.Close();
		}
	}
}