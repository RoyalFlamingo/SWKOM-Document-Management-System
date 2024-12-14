using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Business.Models.Domain;

namespace Business.Services;

public class ElasticService(ILogger<ElasticService> logger, ElasticsearchClient client) : IElasticService
{
	private readonly ILogger<ElasticService> _logger = logger;
	private readonly ElasticsearchClient _client = client;

	public async Task<IndexResponse> IndexDocument(Document document)
	{
		if (document == null || string.IsNullOrWhiteSpace(document.OcrContent))
			throw new ArgumentNullException("Document cannot be null or empty.");

		var indexResponse = await _client.IndexAsync(document, i => i.Index("documents"));
		if (!indexResponse.IsValidResponse)
			throw new InvalidOperationException("Indexing document failed.");

		_logger.LogInformation($"Indexed document {document.Id} with response {indexResponse.ToString()}");

		return indexResponse;
	}

	public async Task<IReadOnlyCollection<Document>?> SearchByFuzzy(string searchTerm)
	{
		if (string.IsNullOrWhiteSpace(searchTerm))
			throw new ArgumentNullException("Search term cannot be empty.");

		var response = await _client.SearchAsync<Document>(s => s
		   .Index("documents")
		   .Query(q => q.Match(m => m
			   .Field(f => f.OcrContent)
			   .Query(searchTerm)
			   .Fuzziness(new Fuzziness(4)))));

		if (response.IsValidResponse)
		{
			if (response.Documents.Any())
			{
				_logger.LogInformation($"Fuzzy search for {searchTerm} resulted in {response.Documents.Count} document{(response.Documents.Count > 1 ? 's' : null)} found.");
				return response.Documents;
			}

			_logger.LogInformation($"Fuzzy search for {searchTerm} resulted in no document found.");
			return null;
		}

		_logger.LogInformation($"Fuzzy search for {searchTerm} resulted in no document found.");
		return null;
	}

	public async Task<IReadOnlyCollection<Document>?> SearchByQueryString(string searchTerm)
	{
		if (string.IsNullOrWhiteSpace(searchTerm))
			throw new ArgumentNullException("Search term cannot be empty.");

		var response = await _client.SearchAsync<Document>(s => s
			.Index("documents")
			.Query(q => q.QueryString(qs => qs.Query($"*{searchTerm}*"))));

		if (response.IsValidResponse)
		{
			if (response.Documents.Any())
			{
				_logger.LogInformation($"Query search for {searchTerm} resulted in {response.Documents.Count} document{(response.Documents.Count > 1 ? 's' : null)} found.");
				return response.Documents;
			}

			_logger.LogInformation($"Query search for {searchTerm} resulted in no document found.");
			return null;
		}

		_logger.LogInformation($"Query search for {searchTerm} resulted in no document found.");
		return null;
	}

	public async Task<bool> DeleteIndexAsync(string indexName)
	{
		var response = await _client.Indices.DeleteAsync(indexName);
		return response.IsValidResponse;
	}

}

