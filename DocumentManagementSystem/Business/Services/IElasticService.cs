using Business.Models.Domain;
using Elastic.Clients.Elasticsearch;

namespace Business.Services;
public interface IElasticService
{
	Task<IndexResponse> IndexDocument(Document document);
	Task<IReadOnlyCollection<Document>?> SearchByQueryString(string searchTerm);
	Task<IReadOnlyCollection<Document>?> SearchByFuzzy(string searchTerm);
}
