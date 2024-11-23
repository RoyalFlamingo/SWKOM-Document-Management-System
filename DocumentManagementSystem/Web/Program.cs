using Swashbuckle.AspNetCore.SwaggerGen;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Persistence;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Business.Services;
using Business.Mapping.Profiles;
using Business.Models.Domain;
using Business.Models.DTO.Validation;
using FluentValidation.AspNetCore;
using FluentValidation;
using Business.Models.Config;
using Minio;

namespace DocumentManagementSystem
{
	public class Program
	{
		[Obsolete]
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// logger
			builder.Logging.ClearProviders();
			builder.Logging.AddConsole();
			builder.Logging.AddDebug();
			builder.Logging.AddEventSourceLogger();

			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowSpecificOrigin",
					builder => builder
						.WithOrigins("http://localhost", "http://localhost:8080")
						.AllowAnyMethod()
						.AllowAnyHeader());
			});

			// Versioning
			builder.Services
				.AddApiVersioning(options =>
			{
				options.DefaultApiVersion = new ApiVersion(1, 0);
				options.AssumeDefaultVersionWhenUnspecified = true;
				options.ReportApiVersions = true;
			})
				.AddMvc()
				.AddApiExplorer(options =>
				 {
					 options.GroupNameFormat = "'v'VVV";
					 options.SubstituteApiVersionInUrl = true;
				 });

			builder.Services.AddRouting(options => options.LowercaseUrls = true);

			// config
			builder.Services.Configure<RabbitMQConfig>(builder.Configuration.GetSection("RabbitMQ"));

			// DB context
			builder.Services.AddDbContext<DocumentsDbContext>(options =>
				options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

			// automapper
			builder.Services.AddAutoMapper(cfg =>
			{
				cfg.AddMaps(new[]
				{
					Assembly.GetExecutingAssembly(),
					typeof(DocumentToDocumentEntityMappingProfile).Assembly // get business assembly
				 });
			});

			// fluentvalidation
			builder.Services.AddFluentValidationAutoValidation();
			builder.Services.AddValidatorsFromAssemblyContaining<DocumentUploadDtoValidator>();

			// Minio
			builder.Services.AddSingleton<IMinioClient>(sp =>
			{
				var config = builder.Configuration.GetSection("MinIO");
				return new MinioClient()
					.WithEndpoint(config["Endpoint"])
					.WithCredentials(config["AccessKey"], config["SecretKey"])
					.Build();
			});
			builder.Services.AddSingleton<IMinioService, MinioService>();

			// Services
			builder.Services.AddScoped<IDocumentService, DocumentService>();
			builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
			builder.Services.AddHostedService<RabbitMqListenerService>();

			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
				{
					Version = "v1",
					Title = "Documents API",
					Description = "API for managing documents (Version 1.0)"
				});

				c.DocInclusionPredicate((version, apiDescription) =>
				{
					if (!apiDescription.TryGetMethodInfo(out var methodInfo)) return false;

					var versions = methodInfo.DeclaringType?
						.GetCustomAttributes(true)
						.OfType<ApiVersionAttribute>()
						.SelectMany(attr => attr.Versions);

					return versions?.Any(v => $"v{v.ToString()}" == version) ?? false;
				});
			});

			var app = builder.Build();

			app.UseCors("AllowSpecificOrigin");

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
				});
			}

			// makes sure to create the db and apply migrations
			using (var scope = app.Services.CreateScope())
			{
				var dbContext = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
				dbContext.Database.Migrate();
			}

			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();

			app.Run();
		}
	}
}
