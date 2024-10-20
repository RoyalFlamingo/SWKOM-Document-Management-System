using System.ComponentModel.DataAnnotations.Schema;

namespace Persistence.Models.Entities
{
	[Table("Users")]
	public record class UserEntity
	{
		public required uint Id { get; set; }
		public required string Name { get; set; }
		public required uint Role { get; set; }
	}
}