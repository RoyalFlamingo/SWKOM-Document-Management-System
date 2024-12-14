using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;

namespace Business.Services;
public class MinioService : IMinioService
{
	private readonly IMinioClient _minioClient;
	private readonly string _bucketName;

	public MinioService(IMinioClient minioClient, IConfiguration configuration)
	{
		_minioClient = minioClient;
		_bucketName = configuration["MinIO:BucketName"];

	}

	public async Task EnsureBucketExistsAsync()
	{
		string bucketName = _bucketName;

		try
		{
			var bktExistArgs = new BucketExistsArgs()
				.WithBucket(bucketName);
			bool found = await _minioClient.BucketExistsAsync(bktExistArgs);

			if (found)
			{
				Console.WriteLine($"{bucketName} already exists.");
			}
			else
			{
				var mkBktArgs = new MakeBucketArgs()
					.WithBucket(bucketName);
				await _minioClient.MakeBucketAsync(mkBktArgs);
				Console.WriteLine($"{bucketName} is created successfully.");
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"Error occurred: {e.Message}");
		}
	}


	public async Task UploadFileAsync(string fileName, Stream data)
	{
		await EnsureBucketExistsAsync();
		await _minioClient.PutObjectAsync(new PutObjectArgs()
			.WithBucket(_bucketName)
			.WithObject(fileName)
			.WithStreamData(data)
			.WithObjectSize(data.Length)
			.WithContentType("application/octet-stream"));
	}

	public async Task<Stream> GetFileAsync(string fileName)
	{
		var memoryStream = new MemoryStream();
		await _minioClient.GetObjectAsync(new GetObjectArgs()
			.WithBucket(_bucketName)
			.WithObject(fileName)
			.WithCallbackStream(stream => stream.CopyTo(memoryStream)));
		memoryStream.Seek(0, SeekOrigin.Begin);
		return memoryStream;
	}

	public async Task DeleteFileAsync(string fileName)
	{
		try
		{
			var removeObjectArgs = new RemoveObjectArgs()
				.WithBucket(_bucketName)
				.WithObject(fileName);

			await _minioClient.RemoveObjectAsync(removeObjectArgs);
			Console.WriteLine($"File {fileName} deleted successfully from bucket {_bucketName}.");
		}
		catch (MinioException ex)
		{
			Console.WriteLine($"Error occurred while deleting file: {ex.Message}");
			throw;
		}
	}
}
