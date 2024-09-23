namespace DocumentManagementSystem.Persistence.Models.Entities
{
	public class Document
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public long Size { get; set; }
		public DateTime UploadedAt { get; set; }
	}
}