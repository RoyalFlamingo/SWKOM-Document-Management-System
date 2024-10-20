using Microsoft.EntityFrameworkCore;
using Persistence;

namespace DocumentManagementSystem.Tests;
public class InMemoryDatabaseFixture : IDisposable
{
	public DocumentsDbContext DbContext { get; private set; }

	public InMemoryDatabaseFixture()
	{
		// InMemoryDatabase configuration
		var options = new DbContextOptionsBuilder<DocumentsDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		DbContext = new DocumentsDbContext(options);
	}

	public void Dispose()
	{
		DbContext.Dispose();
	}
}
