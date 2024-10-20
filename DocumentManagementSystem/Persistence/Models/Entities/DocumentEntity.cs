using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models.Entities
{
	[Table("Documents")]
	public record class DocumentEntity
	{
		public required uint Id { get; set; }
		public required string Name { get; set; }
		public required ulong Size { get; set; }
		public required DateTime UploadedAt { get; set; }
	}
}