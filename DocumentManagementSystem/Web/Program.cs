using Swashbuckle.AspNetCore.SwaggerGen;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace DocumentManagementSystem
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

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

			// Services
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

			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI(c =>
				{
					c.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
				});
			}

			app.UseHttpsRedirection();
			app.UseAuthorization();
			app.MapControllers();

			app.Run();
		}
	}
}
