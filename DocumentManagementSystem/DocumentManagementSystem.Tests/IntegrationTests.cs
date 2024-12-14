using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Persistence.Models.Entities;
using System.Net.Http.Json;

namespace DocumentManagementSystem.Tests
{
	public class IntegrationTests : IAsyncLifetime
	{
		private readonly HttpClient _client;

		public IntegrationTests()
		{
			var handler = new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback =
				   (HttpRequestMessage, certificate, chain, sslPolicyErrors) => true
			};

			var baseAddress = "https://localhost:8081";
			_client = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
		}
		public async Task InitializeAsync()
		{
			await CleanupIndex();
		}
		public async Task DisposeAsync()
		{
			await CleanupIndex();
		}

		private async Task CleanupIndex()
		{
			var response = await _client.DeleteAsync("/api/v1/documents/cleanup-index");
			if (!response.IsSuccessStatusCode)
			{
				throw new Exception("Failed to clean up Elasticsearch index before running tests.");
			}
		}

		[Fact]
		public async Task SaveDocument_ShouldReturn200_WhenDocumentIsValid()
		{
			// Arrange
			var newDocument = new DocumentEntity
			{
				Id = 3,
				Name = "New Test Document",
				OcrContent = "This is a new test document"
			};

			// Act
			var response = await _client.PostAsJsonAsync("/api/v1/documents/elastic-index", newDocument);

			// Assert
			response.EnsureSuccessStatusCode();
			var responseString = await response.Content.ReadAsStringAsync();
			responseString.Should().Contain("Document indexed successfully");
		}

		[Fact]
		public async Task SearchDocument_ShouldReturnDocuments_WhenQueryMatches()
		{
			// Arrange
			var searchTerm = "test";

			await Task.Delay(2000); // 2 sec delay to wait for indexing

			// Act
			var response = await _client.PostAsJsonAsync("/api/v1/documents/query-search", searchTerm);

			// Assert
			response.EnsureSuccessStatusCode();
			var documents = await response.Content.ReadFromJsonAsync<DocumentEntity[]>();

			documents.Should().NotBeNull();
			documents.Should().Contain(d => d.OcrContent.Contains("test"));
			documents.Should().HaveCountGreaterThan(0);
		}
	}
}
