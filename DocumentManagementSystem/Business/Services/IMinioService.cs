namespace Business.Services;
public interface IMinioService
{
	Task UploadFileAsync(string fileName, Stream data);
	Task<Stream> GetFileAsync(string fileName);
	Task DeleteFileAsync(string fileName);

}
